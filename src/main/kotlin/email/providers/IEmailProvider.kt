package com.elwark.notification.email.providers

import com.elwark.notification.email.EmailProviders

interface IEmailProvider {
    val provider: EmailProviders

    suspend fun sendMessage(to: String, subject: String, body: String)
}

