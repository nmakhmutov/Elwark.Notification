package com.elwark.notification

import com.auth0.jwk.JwkProviderBuilder
import com.elwark.notification.api.emailEndpoints
import com.elwark.notification.db.MongoDbContext
import com.elwark.notification.email.EmailBalanceService
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
import io.ktor.client.features.auth.Auth
import io.ktor.client.features.json.JacksonSerializer
import io.ktor.client.features.json.JsonFeature
import io.ktor.client.features.logging.LogLevel
import io.ktor.client.features.logging.Logging
import io.ktor.features.CallLogging
import io.ktor.features.ContentNegotiation
import io.ktor.features.StatusPages
import io.ktor.http.ContentType
import io.ktor.http.HttpStatusCode
import io.ktor.http.URLBuilder
import io.ktor.jackson.jackson
import io.ktor.request.path
import io.ktor.response.respond
import io.ktor.response.respondText
import io.ktor.routing.Routing
import io.ktor.routing.get
import io.ktor.routing.routing
import io.ktor.util.KtorExperimentalAPI
import kotlinx.coroutines.runBlocking
import org.slf4j.event.Level
import java.net.URL
import java.util.concurrent.TimeUnit

fun main(args: Array<String>): Unit = io.ktor.server.netty.EngineMain.main(args)

@KtorExperimentalAPI
@Suppress("unused") // Referenced in application.conf
@kotlin.jvm.JvmOverloads
fun Application.module(testing: Boolean = false) {
    val client = HttpClient(CIO) {
        install(Auth) {
        }
        install(JsonFeature) {
            serializer = JacksonSerializer()
        }
        install(Logging) {
            level = LogLevel.HEADERS
        }
        BrowserUserAgent()
        // install(UserAgent) { agent = "some user agent" }
    }

    val mongo = environment.config.config("mongodb")
    val dbContext = MongoDbContext(mongo.property("connection").getString(), mongo.property("db").getString())
    val emailService = EmailBalanceService(dbContext)

    runBlocking {
        // Sample for making a HTTP Client request
        /*
        val message = client.post<JsonSampleClass> {
            url("http://127.0.0.1:8080/path/to/endpoint")
            contentType(ContentType.Application.Json)
            body = JsonSampleClass(hello = "world")
        }
        */
    }

    val jwkIssuer = environment.config.property("jwk.issuer").getString()
    val jwkProvider = JwkProviderBuilder(jwkUrl(jwkIssuer))
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
        jackson {
        }
    }

    install(CallLogging) {
        level = Level.INFO
        filter { call -> call.request.path().startsWith("/") }
    }

    install(Routing) {
        trace { application.log.trace(it.buildText()) }
        emailEndpoints(emailService)
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

    routing {
        get("/hc") {
            call.respondText("Healthy", contentType = ContentType.Text.Plain)
        }
    }
}

fun Application.jwkUrl(url: String): URL = URL(
    URLBuilder(url)
        .path(".well-known/openid-configuration/jwks")
        .build()
        .toString()
)