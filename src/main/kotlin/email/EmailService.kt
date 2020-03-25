package com.elwark.notification.email

import com.elwark.notification.db.EmailProviderModel
import com.elwark.notification.db.MongoDbContext
import org.litote.kmongo.*

class EmailService(private val dbContext: MongoDbContext) {
    suspend fun getAll(): Iterable<ProviderBalanceDto> {
        val result = dbContext.emailProviders.find().toList()

        return result.map { ProviderBalanceDto(it.name, it.dailyLimit, it.balance, it.lastUsedAt) }
    }

    suspend fun updateDailyLimit(provider: EmailProviders, limit: Int) {
        var updated: Long
        do {
            val db = dbContext.emailProviders.findOne(EmailProviderModel::name eq provider)
                ?: return

            val balance = if (db.dailyLimit > limit) {
                val tmp = db.balance - (db.dailyLimit - limit)
                if (tmp < 0) {
                    0
                } else {
                    tmp
                }
            }
            else {
                db.balance + (limit - db.dailyLimit)
            }

            updated = dbContext.emailProviders.updateOne(
                and(
                    EmailProviderModel::name eq provider,
                    EmailProviderModel::version eq db.version
                ),
                combine(
                    setValue(EmailProviderModel::dailyLimit, limit),
                    setValue(EmailProviderModel::balance, balance),
                    inc(EmailProviderModel::version, 1)
                )
            ).modifiedCount

        } while (updated == 0L)
    }
}