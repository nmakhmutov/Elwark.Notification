package com.elwark.notification.email

import com.fasterxml.jackson.databind.annotation.JsonSerialize
import com.fasterxml.jackson.databind.ser.std.ToStringSerializer
import java.time.LocalDateTime

data class ProviderBalanceResponse(
    val provider: EmailProviders,
    val count: Int,
    @JsonSerialize(using = ToStringSerializer::class)
    val lastUsedAt: LocalDateTime
)

