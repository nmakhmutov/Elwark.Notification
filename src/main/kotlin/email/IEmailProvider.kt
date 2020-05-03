package com.elwark.notification.email

interface IEmailProvider {
    val provider: ProviderType

    suspend fun sendMessage(to: String, subject: String, body: String)
}

