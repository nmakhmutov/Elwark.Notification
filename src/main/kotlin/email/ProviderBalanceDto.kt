package com.elwark.notification.email

import java.time.LocalDateTime

data class ProviderBalanceDto(
    val provider: ProviderType,
    val limit: Int,
    val balance: Int,
    val updateInterval: Long,
    val updateAt: LocalDateTime,
    val lastUsedAt: LocalDateTime
)

