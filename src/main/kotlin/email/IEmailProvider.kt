package com.elwark.notification.email

interface IEmailProvider {
    val provider: ProviderType

    suspend fun sendMessage(to: String, subject: String, body: String): EmailProviderResponse
}

data class EmailProviderResponse(val status: String, val message: String, val body: String?)
