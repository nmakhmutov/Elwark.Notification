package com.elwark.notification.email

import com.elwark.notification.db.EmailProviderModel
import com.elwark.notification.db.MongoDbContext
import org.litote.kmongo.*
import java.lang.Exception
import java.time.LocalDateTime
import java.time.ZoneOffset

class EmailBalanceService(private val dbContext: MongoDbContext) {
    suspend fun getAll(): Iterable<ProviderBalanceDto> {
        val result = dbContext.emailProviders.find().toList()

        return result.map { ProviderBalanceDto(it.name, it.dailyLimit, it.balance, it.lastUsedAt) }
    }

    suspend fun updateDailyLimit(provider: EmailProviders, limit: Int) {
        while (true) {
            val model = dbContext.emailProviders.findOne(EmailProviderModel::name eq provider)
                ?: return

            val balance = if (model.dailyLimit > limit) {
                val tmp = model.balance - (model.dailyLimit - limit)
                if (tmp < 0) {
                    0
                } else {
                    tmp
                }
            } else {
                model.balance + (limit - model.dailyLimit)
            }

            val result = dbContext.emailProviders.updateOne(
                and(
                    EmailProviderModel::name eq provider,
                    EmailProviderModel::version eq model.version
                ),
                combine(
                    setValue(EmailProviderModel::dailyLimit, limit),
                    setValue(EmailProviderModel::balance, balance),
                    inc(EmailProviderModel::version, 1)
                )
            )

            if(result.modifiedCount > 0)
                return
        }
    }

    suspend fun getNext(): EmailProviders {
        while (true) {
            val model = dbContext.emailProviders
                .find(EmailProviderModel:: balance gt 0)
                .sort(descending(EmailProviderModel::balance))
                .first() ?: throw Exception("Out of daily limit")

            val update = dbContext.emailProviders.updateOne(
                and(
                    EmailProviderModel::name eq model.name,
                    EmailProviderModel::version eq model.version
                ),
                combine(
                    inc(EmailProviderModel::balance, -1),
                    inc(EmailProviderModel::version, 1),
                    setValue(EmailProviderModel::lastUsedAt, LocalDateTime.now(ZoneOffset.UTC))
                )
            )

            if(update.modifiedCount > 0)
                return model.name
        }
    }
}