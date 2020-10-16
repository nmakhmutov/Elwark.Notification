package com.elwark.notification.email.providers

import com.elwark.notification.email.EmailProviderResponse
import com.elwark.notification.email.IEmailProvider
import com.elwark.notification.email.ProviderType
import io.ktor.client.HttpClient
import io.ktor.client.request.header
import io.ktor.client.request.post
import io.ktor.client.request.url
import io.ktor.client.statement.HttpResponse
import io.ktor.client.statement.readText
import io.ktor.http.HttpHeaders
import io.ktor.http.URLBuilder
import io.ktor.http.isSuccess
import org.slf4j.LoggerFactory

data class SendgridOptions(val host: String, val key: String)

class Sendgrid(private val client: HttpClient, private val options: SendgridOptions) : IEmailProvider {

    private val sendUrl = URLBuilder(options.host)
        .path("v3/mail/send")
        .buildString()

    override val provider: ProviderType =
        ProviderType.Sendgrid

    override suspend fun sendMessage(to: String, subject: String, body: String): EmailProviderResponse {
        val message = Message(
            listOf(
                Personalization(listOf(Email(to)))
            ),
            Email("elwarkinc@gmail.com", "Elwark"),
            subject,
            listOf(Content("text/html", body))
        )

        val response = client.post<HttpResponse> {
            url(sendUrl)
            header(HttpHeaders.ContentType, "application/json")
            header(HttpHeaders.Authorization, "Bearer ${options.key}")

            this.body = message
        }

        return EmailProviderResponse(response.status.value.toString(), response.status.description, response.readText())
    }

    private data class Message(
        val personalizations: List<Personalization>,
        val from: Email,
        val subject: String,
        val content: List<Content>
    )

    private data class Personalization(val to: List<Email>)

    data class Content(val type: String, val value: String)

    private data class Email(val email: String, val name: String? = null)
}