package com.elwark.notification.services

import org.apache.commons.mail.DefaultAuthenticator
import org.apache.commons.mail.EmailConstants
import org.apache.commons.mail.HtmlEmail
import org.apache.commons.mail.SimpleEmail

data class EmailSenderSetting(
    val host: String,
    val port: Int,
    val username: String,
    val password: String,
    val isSSL: Boolean
)

class EmailSender(val settings: EmailSenderSetting) {
    fun send(email: String, subject: String, body: String, isHtml: Boolean = true) {
        val message = if (isHtml)
            HtmlEmail()
                .apply {
                    setHostName(settings.host)
                    setSmtpPort(settings.port)
                    setAuthenticator(DefaultAuthenticator(settings.username, settings.password))
                    setSSLOnConnect(settings.isSSL)
                    setFrom("elwarkinc@gmail.com", "Elwark")
                    setSubject(subject)
                    setHtmlMsg(body)
                    setCharset(EmailConstants.UTF_8)
                    addTo(email)
                }
        else
            SimpleEmail()
                .apply {
                    setHostName(settings.host)
                    setSmtpPort(settings.port)
                    setAuthenticator(DefaultAuthenticator(settings.username, settings.password))
                    setSSLOnConnect(true)
                    setFrom("elwarkinc@gmail.com", "Elwark")
                    setSubject(subject)
                    setMsg(body)
                    setCharset(EmailConstants.UTF_8)
                    addTo(email)
                }

        message.send();
    }
}