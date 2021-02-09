package com.elwark.notification.models

import org.bson.types.ObjectId
import org.springframework.data.annotation.Id
import org.springframework.data.mongodb.core.index.CompoundIndex
import org.springframework.data.mongodb.core.index.CompoundIndexes
import org.springframework.data.mongodb.core.index.Indexed
import org.springframework.data.mongodb.core.mapping.Document
import java.time.LocalDateTime

@Document(collection = "email_providers")
data class EmailProvider(
    @Id
    val id: ObjectId = ObjectId.get(),
    val type: EmailProviderType,
    val limit: Int,
    val balance: Int,
    val updateInterval: Int,
    val updatedAt: LocalDateTime,
    val lastUsedAt: LocalDateTime = LocalDateTime.of(2000, 1, 1, 0, 0),
    val version: Int = Int.MIN_VALUE
)