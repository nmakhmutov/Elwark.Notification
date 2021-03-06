package com.elwark.notification.events

import com.elwark.notification.email.EmailProviderFactory
import com.google.gson.Gson
import com.rabbitmq.client.AMQP
import com.rabbitmq.client.Channel
import com.rabbitmq.client.DefaultConsumer
import com.rabbitmq.client.Envelope
import io.ktor.client.features.ClientRequestException
import io.ktor.client.features.ResponseException
import io.ktor.client.statement.readText
import io.ktor.client.statement.request
import kotlinx.coroutines.runBlocking
import org.slf4j.LoggerFactory
import java.lang.StringBuilder
import java.util.*

data class EmailCreatedIntegrationEvent(val id: UUID, val email: String?, val subject: String, val body: String)

class EmailCreatedEventHandler(
    channel: Channel,
    private val emailProviderFactory: EmailProviderFactory,
    private val gson: Gson
) :
    DefaultConsumer(channel) {
    private val logger = LoggerFactory.getLogger(EmailCreatedEventHandler::class.java)

    override fun handleDelivery(
        consumerTag: String?,
        envelope: Envelope,
        properties: AMQP.BasicProperties?,
        body: ByteArray?
    ) {
        val message = body?.let {
            gson.fromJson(String(it), EmailCreatedIntegrationEvent::class.java)
        } ?: return

        logger.info("Received message ${message.id} for email ${message.email}")

        if (message.email == null) {
            logger.warn("Received empty email")
            return channel.basicAck(envelope.deliveryTag, false);
        }

        val sb = StringBuilder()
        sb.appendln("Sending email to '${message.email}' with subject '${message.subject}'")

        return try {
            runBlocking {
                val provider = emailProviderFactory.get()
                sb.appendln("Next email provider is ${provider.provider}")

                val response = provider.sendMessage(message.email, message.subject, message.body)

                sb.appendln("${provider.provider} respond status code ${response.status} (${response.message})")

                if (response.body != null)
                    sb.appendln("Response body: ${response.body}")
            }

            channel.basicAck(envelope.deliveryTag, false)
        } catch (ex: Exception) {
            logger.error(ex.toString())
            sb.appendln("Error occurred ${ex.message}")

            channel.basicReject(envelope.deliveryTag, true)
        } finally {
            logger.info(sb.toString())
        }
    }
}