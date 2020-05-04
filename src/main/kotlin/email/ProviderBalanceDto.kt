package com.elwark.notification.email

import java.time.LocalDateTime

data class ProviderBalanceDto(
    val provider: ProviderType,
    val dailyLimit: Int,
    val balance: Int,
    val lastUsedAt: LocalDateTime
)

