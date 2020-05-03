package com.elwark.notification.db

import com.elwark.notification.email.ProviderType
import org.bson.codecs.pojo.annotations.BsonId
import org.litote.kmongo.Id
import org.litote.kmongo.newId
import java.time.LocalDateTime

data class ProviderModel(
    val name: ProviderType,
    val dailyLimit: Int,
    val balance: Int,
    val lastUsedAt: LocalDateTime,
    val version: Long
) {
    @BsonId
    val id: Id<ProviderModel> = newId()
}