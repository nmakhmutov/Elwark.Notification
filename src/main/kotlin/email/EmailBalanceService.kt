package com.elwark.notification.email

import com.elwark.notification.db.ProviderModel
import com.elwark.notification.db.MongoDbContext
import org.litote.kmongo.*
import org.slf4j.LoggerFactory
import java.lang.Exception
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.ZoneOffset

class EmailBalanceService(private val dbContext: MongoDbContext) {
    private val logger = LoggerFactory.getLogger(EmailBalanceService::class.qualifiedName)

    private fun ProviderModel.toDto(): ProviderBalanceDto =
        ProviderBalanceDto(
            this.type,
            this.limit,
            this.balance,
            this.updateInterval,
            this.updatedAt,
            this.lastUsedAt
        )

    suspend fun getAll(): List<ProviderBalanceDto> {
        val result = dbContext.emailProviders.find().toList()

        return result.map { it.toDto() }
    }

    suspend fun get(provider: ProviderType): ProviderBalanceDto {
        val result = dbContext.emailProviders.findOne(ProviderModel::type eq provider)

        return result?.toDto() ?: throw IllegalArgumentException("Not found provider '$provider'")
    }

    suspend fun update(provider: ProviderType, limit: Int, interval: UpdateInterval, updateAt: LocalDate) {
        while (true) {
            val model = dbContext.emailProviders.findOne(ProviderModel::type eq provider)
                ?: return

            val balance = if (model.limit > limit) {
                val tmp = model.balance - (model.limit - limit)
                if (tmp < 0) {
                    0
                } else {
                    tmp
                }
            } else {
                model.balance + (limit - model.limit)
            }

            val result = dbContext.emailProviders.updateOne(
                and(
                    ProviderModel::type eq provider,
                    ProviderModel::version eq model.version
                ),
                combine(
                    setValue(ProviderModel::limit, limit),
                    setValue(ProviderModel::balance, balance),
                    setValue(
                        ProviderModel::updatedAt,
                        LocalDateTime.of(updateAt.year, updateAt.month, updateAt.dayOfMonth, 0, 0, 0)
                    ),
                    setValue(ProviderModel::updateInterval, interval),
                    inc(ProviderModel::version, 1)
                )
            )

            if (result.modifiedCount > 0)
                return
        }
    }

    suspend fun checkAndResetBalances() {
        val providers = dbContext.emailProviders
            .find(ProviderModel::updatedAt lte LocalDateTime.now(ZoneOffset.UTC))
            .toList()

        if (providers.count() == 0)
            return

        providers.forEach { provider ->
            val updateAt = when (provider.updateInterval) {
                UpdateInterval.Daily -> provider.updatedAt.plusDays(1)
                UpdateInterval.Monthly -> provider.updatedAt.plusMonths(1)
            }

            dbContext.emailProviders.updateOne(
                ProviderModel::type eq provider.type,
                combine(
                    setValue(ProviderModel::balance, provider.limit),
                    setValue(ProviderModel::updatedAt, updateAt)
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