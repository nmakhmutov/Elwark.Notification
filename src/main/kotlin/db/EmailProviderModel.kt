package com.elwark.notification.db

import com.elwark.notification.email.EmailProviders
import org.bson.codecs.pojo.annotations.BsonId
import org.litote.kmongo.Id
import org.litote.kmongo.newId
import java.time.LocalDateTime

data class EmailProviderModel(
    val name: EmailProviders,
    val dailyLimit: Int,
    val balance: Int,
    val lastUsedAt: LocalDateTime,
    val version: Long
) {
    @BsonId
    val id: Id<EmailProviderModel> = newId()
}