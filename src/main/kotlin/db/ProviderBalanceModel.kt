package com.elwark.notification.db

import com.elwark.notification.email.EmailProviders
import java.time.LocalDateTime

data class ProviderBalanceModel(
    val provider: EmailProviders,
    val dailyLimit: Int,
    val currentLimit: Int,
    val lastUsedAt: LocalDateTime
)