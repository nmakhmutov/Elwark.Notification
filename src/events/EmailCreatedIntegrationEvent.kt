package com.elwark.notification.events

import com.google.gson.annotations.SerializedName
import java.time.LocalDateTime
import java.util.*

data class EmailCreatedIntegrationEvent(
    @SerializedName("id")
    val Id: UUID,

    @SerializedName("creationDate")
    val CreationDate: LocalDateTime,

    @SerializedName("email")
    val Email: String,

    @SerializedName("subject")
    val Subject: String,

    @SerializedName("body")
    val Body: String,

    @SerializedName("isHtml")
    val IsHtml: Boolean
)