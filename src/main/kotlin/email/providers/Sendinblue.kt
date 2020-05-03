package com.elwark.notification.email.providers

import com.elwark.notification.email.EmailProviders
import io.ktor.client.HttpClient
import io.ktor.client.request.header
import io.ktor.client.request.post
import io.ktor.client.request.url
import io.ktor.http.ContentType
import io.ktor.http.URLBuilder
import io.ktor.http.contentType

data class SendinblueOptions(val host: String, val key: String)

class Sendinblue(private val client: HttpClient, private val options: SendinblueOptions) : IEmailProvider {

    private val sendUrl = URLBuilder(options.host)
        .path("v3/smtp/email")
        .buildString()

    override val provider: EmailProviders =
        EmailProviders.Sendinblue

    override suspend fun sendMessage(to: String, subject: String, body: String) {
        val message = Message(
            Email("elwarkinc@gmail.com", "Elwark"),
            listOf(Email(to)),
            subject,
            body
        )

        client.post<Unit> {
            url(sendUrl)
            contentType(ContentType.Application.Json)
            header("api-key", options.key)

            this.body = message
        }
    }

    private data class Message(
        val sender: Email,
        val to: Iterable<Email>,
        val subject: String,
        val htmlContent: String
    )

    private data class Email(val email: String, val name: String? = null)
}