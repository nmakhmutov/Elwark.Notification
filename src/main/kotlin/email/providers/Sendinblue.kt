package com.elwark.notification.email.providers

import com.elwark.notification.email.EmailProviderResponse
import com.elwark.notification.email.IEmailProvider
import com.elwark.notification.email.ProviderType
import io.ktor.client.HttpClient
import io.ktor.client.request.header
import io.ktor.client.request.post
import io.ktor.client.request.url
import io.ktor.http.HttpHeaders
import io.ktor.http.URLBuilder
import io.ktor.client.statement.HttpResponse
import io.ktor.client.statement.readText

data class SendinblueOptions(val host: String, val key: String)

class Sendinblue(private val client: HttpClient, private val options: SendinblueOptions) : IEmailProvider {

    private val sendUrl = URLBuilder(options.host)
        .path("v3/smtp/email")
        .buildString()

    override val provider: ProviderType =
        ProviderType.Sendinblue

    override suspend fun sendMessage(to: String, subject: String, body: String): EmailProviderResponse {
        val message = Message(
            Email("elwarkinc@gmail.com", "Elwark"),
            listOf(Email(to)),
            subject,
            body
        )

        val response = client.post<HttpResponse> {
            url(sendUrl)
            header(HttpHeaders.ContentType, "application/json")
            header("api-key", options.key)

            this.body = message
        }

        return EmailProviderResponse(response.status.value.toString(), response.status.description, response.readText())
    }

    private data class Message(
        val sender: Email,
        val to: List<Email>,
        val subject: String,
        val htmlContent: String
    )

    private data class Email(val email: String, val name: String? = null)
}