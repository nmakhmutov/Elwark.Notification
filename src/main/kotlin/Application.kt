package com.elwark.notification

import com.auth0.jwk.JwkProviderBuilder
import com.elwark.notification.api.providersEndpoints
import com.elwark.notification.converters.LocalDateConverter
import com.elwark.notification.converters.LocalDateTimeConverter
import com.elwark.notification.db.MongoDbContext
import com.elwark.notification.email.EmailBalanceService
import com.elwark.notification.email.EmailProviderFactory
import com.elwark.notification.email.providers.Sendgrid
import com.elwark.notification.email.providers.SendgridOptions
import com.elwark.notification.email.providers.Sendinblue
import com.elwark.notification.email.providers.SendinblueOptions
import com.elwark.notification.events.EmailCreatedEventHandler
import com.elwark.notification.events.EmailCreatedIntegrationEvent
import com.google.gson.GsonBuilder
import com.rabbitmq.client.BuiltinExchangeType
import com.rabbitmq.client.ConnectionFactory
import io.ktor.application.Application
import io.ktor.application.call
import io.ktor.application.install
import io.ktor.application.log
import io.ktor.auth.Authentication
import io.ktor.auth.jwt.JWTPrincipal
import io.ktor.auth.jwt.jwt
import io.ktor.client.HttpClient
import io.ktor.client.engine.cio.CIO
import io.ktor.client.features.BrowserUserAgent
import io.ktor.client.features.json.GsonSerializer
import io.ktor.client.features.json.JsonFeature
import io.ktor.client.features.logging.DEFAULT
import io.ktor.client.features.logging.LogLevel
import io.ktor.client.features.logging.Logger
import io.ktor.client.features.logging.Logging
import io.ktor.features.CallLogging
import io.ktor.features.ContentNegotiation
import io.ktor.features.StatusPages
import io.ktor.gson.gson
import io.ktor.http.ContentType
import io.ktor.http.HttpStatusCode
import io.ktor.request.path
import io.ktor.response.respond
import io.ktor.response.respondText
import io.ktor.routing.Routing
import io.ktor.routing.get
import io.ktor.util.KtorExperimentalAPI
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import org.slf4j.event.Level
import java.net.URL
import java.time.Duration
import java.time.LocalDate
import java.time.LocalDateTime
import java.util.concurrent.TimeUnit

fun main(args: Array<String>): Unit = io.ktor.server.netty.EngineMain.main(args)

@KtorExperimentalAPI
@Suppress("unused") // Referenced in application.conf
@kotlin.jvm.JvmOverloads
fun Application.module(testing: Boolean = false) {
    val serviceName = "Elwark.Notification"

    val gsonBuilder = GsonBuilder()
        .registerTypeAdapter(LocalDate::class.java, LocalDateConverter())
        .registerTypeAdapter(LocalDateTime::class.java, LocalDateTimeConverter())


    val client = HttpClient(CIO) {
        install(JsonFeature) {
            serializer = GsonSerializer {
                registerTypeAdapter(LocalDate::class.java, LocalDateConverter())
                registerTypeAdapter(LocalDateTime::class.java, LocalDateTimeConverter())
            }
        }
        install(Logging) {
            level = LogLevel.HEADERS
            logger = Logger.DEFAULT
        }
        BrowserUserAgent()
    }

    val dbContext = MongoDbContext(
        environment.config.property("mongodb.connection").getString(),
        environment.config.property("mongodb.db").getString()
    )
    val emailBalanceService = EmailBalanceService(dbContext)
    val emailProviderFactory = EmailProviderFactory(
        emailBalanceService,
        listOf(
            Sendgrid(
                client,
                SendgridOptions(
                    environment.config.property("sendgrid.host").getString(),
                    environment.config.property("sendgrid.key").getString()
                )
            ),
            Sendinblue(
                client,
                SendinblueOptions(
                    environment.config.property("sendinblue.host").getString(),
                    environment.config.property("sendinblue.key").getString()
                )
            )
        )
    )

    val jwkIssuer = environment.config.property("jwk.issuer").getString()
    val jwkProvider = JwkProviderBuilder(URL(environment.config.property("jwk.url").getString()))
        .cached(10, 24, TimeUnit.HOURS)
        .rateLimited(10, 1, TimeUnit.MINUTES)
        .build()

    install(Authentication) {
        jwt {
            verifier(jwkProvider, jwkIssuer)
            realm = "jwk auth"
            validate { credentials ->
                if (credentials.payload.audience.contains(environment.config.property("jwk.audience").getString()))
                    JWTPrincipal(credentials.payload)
                else
                    null
            }
        }
    }

    install(ContentNegotiation) {
        gson {
            registerTypeAdapter(LocalDate::class.java, LocalDateConverter())
            registerTypeAdapter(LocalDateTime::class.java, LocalDateTimeConverter())
        }
    }

    install(CallLogging) {
        level = Level.INFO
        filter { call -> call.request.path().startsWith("/") }
    }

    install(Routing) {
        trace { application.log.trace(it.buildText()) }
        get("/hc") { call.respondText("Healthy", contentType = ContentType.Text.Plain) }
        providersEndpoints(emailBalanceService)
    }

    install(StatusPages) {
        this.exception<Throwable> {
            call.respond(HttpStatusCode.BadRequest, object {
                val title = it::class.java.name
                val message = it.localizedMessage
            })
            throw it
        }
    }

    val factory = ConnectionFactory()
    factory.host = environment.config.property("rabbitmq.host").getString()
    factory.username = environment.config.property("rabbitmq.username").getString()
    factory.password = environment.config.property("rabbitmq.password").getString()
    factory.virtualHost = environment.config.property("rabbitmq.virtualHost").getString()
    factory.port = environment.config.property("rabbitmq.port").getString().toInt()

    val exchangeName = environment.config.property("rabbitmq.exchange").getString()
    val connection = factory.newConnection(serviceName)

    val channel = connection.createChannel()
    channel.exchangeDeclare(exchangeName, BuiltinExchangeType.DIRECT, true, false, null)

    val queueName = "$serviceName:${EmailCreatedIntegrationEvent::class.simpleName}"
    val queue = channel.queueDeclare(queueName, true, false, false, null)
    channel.queueBind(queue.queue, exchangeName, EmailCreatedIntegrationEvent::class.simpleName)
    channel.basicConsume(
        queue.queue,
        false,
        EmailCreatedEventHandler(
            channel,
            emailProviderFactory,
            gsonBuilder.create()
        )
    )

    launch {
        while (true) {
            emailBalanceService.checkAndResetBalances()
            delay(Duration.ofMinutes(1).toMillis())
        }
    }
}