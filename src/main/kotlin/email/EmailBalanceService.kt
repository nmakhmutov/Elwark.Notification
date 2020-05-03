package com.elwark.notification.email

import com.elwark.notification.db.ProviderModel
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

    suspend fun updateDailyLimit(provider: ProviderType, limit: Int) {
        while (true) {
            val model = dbContext.emailProviders.findOne(ProviderModel::name eq provider)
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
                    ProviderModel::name eq provider,
                    ProviderModel::version eq model.version
                ),
                combine(
                    setValue(ProviderModel::dailyLimit, limit),
                    setValue(ProviderModel::balance, balance),
                    inc(ProviderModel::version, 1)
                )
            )

            if(result.modifiedCount > 0)
                return
        }
    }

    suspend fun getNext(): ProviderType {
        while (true) {
            val model = dbContext.emailProviders
                .find(ProviderModel:: balance gt 0)
                .sort(descending(ProviderModel::balance))
                .first() ?: throw Exception("Out of daily limit")

            val update = dbContext.emailProviders.updateOne(
                and(
                    ProviderModel::name eq model.name,
                    ProviderModel::version eq model.version
                ),
                combine(
                    inc(ProviderModel::balance, -1),
                    inc(ProviderModel::version, 1),
                    setValue(ProviderModel::lastUsedAt, LocalDateTime.now(ZoneOffset.UTC))
                )
            )

            if(update.modifiedCount > 0)
                return model.name
        }
    }
}