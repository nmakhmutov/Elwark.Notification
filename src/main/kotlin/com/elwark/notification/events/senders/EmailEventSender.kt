package com.elwark.notification.events.senders

import com.elwark.notification.KafkaTopic
import com.elwark.notification.events.EmailEvent
import com.elwark.notification.infrastructure.repository.EmailProviderRepository
import com.elwark.notification.models.EmailProvider
import org.slf4j.Logger
import org.slf4j.LoggerFactory
import org.springframework.data.domain.Sort
import org.springframework.data.mongodb.core.ReactiveMongoTemplate
import org.springframework.kafka.core.KafkaTemplate
import org.springframework.stereotype.Service

@Service
class EmailEventSender(val kafka: KafkaTemplate<String, EmailEvent>) {
    private val logger: Logger = LoggerFactory.getLogger(EmailEventSender::class.java)

    fun send(email: EmailEvent) {
        logger.info("Received email with subject '${email.subject}' for email '${email.email}'")
        kafka.send(KafkaTopic.Emails, email)
    }
}