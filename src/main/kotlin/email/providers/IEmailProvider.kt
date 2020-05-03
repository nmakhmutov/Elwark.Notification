package com.elwark.notification.email.providers

import com.elwark.notification.email.EmailProviders

interface IEmailProvider {
    val provider: EmailProviders

    suspend fun sendHtmlMessage(to: String, subject: String, body: String)
    suspend fun sendTextMessage(to: String, subject: String, body: String)
}

