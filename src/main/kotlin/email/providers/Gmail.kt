package com.elwark.notification.email.providers

import com.elwark.notification.email.EmailProviderResponse
import com.elwark.notification.email.IEmailProvider
import com.elwark.notification.email.ProviderType
import org.apache.commons.mail.DefaultAuthenticator
import org.apache.commons.mail.ImageHtmlEmail

data class GmailOptions(val host: String, val port: Int, val username: String, val password: String)

class Gmail(private val options: GmailOptions) : IEmailProvider {

    override val provider: ProviderType = ProviderType.Gmail

    override suspend fun sendMessage(to: String, subject: String, body: String): EmailProviderResponse {
        val email = ImageHtmlEmail()
            .apply {
                hostName = options.host
                isSSLOnConnect = true
                this.subject = subject
            }

        email.setSmtpPort(options.port)
        email.setAuthenticator(DefaultAuthenticator(options.username, options.password))
        email.setFrom(options.username)
        email.setMsg(body)
        email.addTo(to)

        val response = email.send()

        return EmailProviderResponse("Ok", response, null)
    }
}