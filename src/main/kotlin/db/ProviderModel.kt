package com.elwark.notification.db

import com.elwark.notification.email.ProviderType
import com.elwark.notification.email.UpdateInterval
import org.bson.codecs.pojo.annotations.BsonId
import org.litote.kmongo.Id
import org.litote.kmongo.newId
import java.time.LocalDateTime

data class ProviderModel(
    val type: ProviderType,
    val limit: Int,
    val balance: Int,
    val updateInterval: UpdateInterval,
    val updatedAt: LocalDateTime
) {
    @BsonId
    val id: Id<ProviderModel> = newId()
    val version: Long = Long.MIN_VALUE
    val lastUsedAt: LocalDateTime = LocalDateTime.of(2000, 1, 1, 0, 0)
}