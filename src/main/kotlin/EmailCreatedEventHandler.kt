package com.elwark.notification

import com.elwark.notification.email.EmailProviderFactory
import com.google.gson.Gson
import com.rabbitmq.client.AMQP
import com.rabbitmq.client.Channel
import com.rabbitmq.client.DefaultConsumer
import com.rabbitmq.client.Envelope
import kotlinx.coroutines.runBlocking
import org.slf4j.LoggerFactory
import java.util.*

data class EmailCreatedIntegrationEvent(val id: UUID, val email: String, val subject: String, val body: String)

class EmailCreatedEventHandler(channel: Channel, val emailProviderFactory: EmailProviderFactory, val gson: Gson) :
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

        logger.debug("Received message ${message.id}")

        try {
            runBlocking {
                val provider = emailProviderFactory.get()

                logger.debug("Next email sender is ${provider.provider}")

                provider.sendMessage(message.email, message.subject, message.body)
            }

            channel.basicAck(envelope.deliveryTag, false)
        } catch (ex: Exception) {
            channel.basicNack(envelope.deliveryTag, false, false)
        }
    }
}