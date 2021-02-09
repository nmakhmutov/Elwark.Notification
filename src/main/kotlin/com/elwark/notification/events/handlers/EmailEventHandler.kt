package com.elwark.notification.events.handlers

import com.elwark.notification.KafkaTopic
import com.elwark.notification.events.EmailEvent
import com.elwark.notification.infrastructure.repository.EmailProviderRepository
import com.elwark.notification.models.EmailProvider
import org.slf4j.LoggerFactory
import org.springframework.data.domain.Sort
import org.springframework.data.mongodb.core.MongoTemplate
import org.springframework.kafka.annotation.KafkaListener
import org.springframework.stereotype.Service

@Service
class EmailEventHandler(val repository: EmailProviderRepository, val template: MongoTemplate) {
    private val logger = LoggerFactory.getLogger(EmailEventHandler::class.java)

    @KafkaListener(topics = [KafkaTopic.Emails], groupId = "notification.email_sender")
    fun consume(message: EmailEvent) {
        val t = repository.findAll(Sort.by(Sort.Direction.DESC, EmailProvider::limit.name))[0]

        logger.info("Message for email ${message.email} sent. ${t}")
    }
}