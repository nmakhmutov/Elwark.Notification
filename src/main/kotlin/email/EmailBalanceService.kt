package com.elwark.notification.email

import com.elwark.notification.db.ProviderModel
import com.elwark.notification.db.MongoDbContext
import org.litote.kmongo.*
import org.slf4j.LoggerFactory
import java.lang.Exception
import java.time.LocalDateTime
import java.time.ZoneOffset

class EmailBalanceService(private val dbContext: MongoDbContext) {
    private val logger = LoggerFactory.getLogger(EmailBalanceService::class.qualifiedName)

    suspend fun getAll(): Iterable<ProviderBalanceDto> {
        val result = dbContext.emailProviders.find().toList()

        return result.map { ProviderBalanceDto(it.type, it.dailyLimit, it.balance, it.lastUsedAt) }
    }

    suspend fun updateDailyLimit(provider: ProviderType, limit: Int) {
        while (true) {
            val model = dbContext.emailProviders.findOne(ProviderModel::type eq provider)
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
                    ProviderModel::type eq provider,
                    ProviderModel::version eq model.version
                ),
                combine(
                    setValue(ProviderModel::dailyLimit, limit),
                    setValue(ProviderModel::balance, balance),
                    inc(ProviderModel::version, 1)
                )
            )

            if (result.modifiedCount > 0)
                return
        }
    }

    suspend fun checkAndResetBalances() {
        val providers = dbContext.emailProviders
            .find(ProviderModel::updateAt lte LocalDateTime.now(ZoneOffset.UTC))
            .toList()

        if (providers.count() == 0)
            return

        for (provider in providers) {
            dbContext.emailProviders.updateOne(
                ProviderModel::type eq provider.type,
                combine(
                    setValue(ProviderModel::balance, provider.dailyLimit),
                    setValue(ProviderModel::updateAt, provider.updateAt.plusSeconds(provider.updateInterval))
                )
            )

            logger.info("Updated daily limit for provider ${provider.type}")
        }
    }

    suspend fun getNext(): ProviderType {
        while (true) {
            val model = dbContext.emailProviders
                .find(ProviderModel::balance gt 0)
                .sort(descending(ProviderModel::balance))
                .first() ?: throw Exception("Out of daily limit")

            val update = dbContext.emailProviders.updateOne(
                and(
                    ProviderModel::type eq model.type,
                    ProviderModel::version eq model.version
                ),
                combine(
                    inc(ProviderModel::balance, -1),
                    inc(ProviderModel::version, 1),
                    setValue(ProviderModel::lastUsedAt, LocalDateTime.now(ZoneOffset.UTC))
                )
            )

            if (update.modifiedCount > 0)
                return model.type
        }
    }
}