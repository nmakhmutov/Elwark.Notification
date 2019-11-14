package com.elwark.notification.eventbus

import com.rabbitmq.client.Channel
import com.rabbitmq.client.Connection
import com.rabbitmq.client.ConnectionFactory

data class RabbitMQSettings(
    val host: String,
    val username: String,
    val password: String,
    val virtualHost: String,
    val connectionName: String
)

class RabbitMQConnection(setting: RabbitMQSettings) {
    private var connection: Connection

    init {
        val factory = ConnectionFactory()
        factory.host = setting.host
        factory.username = setting.username
        factory.password = setting.password
        factory.virtualHost = setting.virtualHost

        connection = factory.newConnection(setting.connectionName)
    }

    fun createChannel(): Channel =
        connection.createChannel()
}