package com.elwark.notification

import com.elwark.notification.api.emailEndpoints
import com.elwark.notification.db.MongoDbContext
import com.elwark.notification.email.EmailProviders
import com.elwark.notification.db.ProviderBalanceModel
import io.ktor.application.*
import io.ktor.response.*
import io.ktor.routing.*
import io.ktor.http.*
import io.ktor.auth.*
import io.ktor.features.*
import io.ktor.client.*
import io.ktor.client.engine.cio.*
import io.ktor.client.features.auth.*
import io.ktor.client.features.json.*
import kotlinx.coroutines.*
import io.ktor.client.features.logging.*
import io.ktor.client.features.BrowserUserAgent
import io.ktor.jackson.jackson
import io.ktor.util.KtorExperimentalAPI
import org.litote.kmongo.eq

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

    runBlocking {
        val provider = dbContext.emailProviders.findOne(ProviderBalanceModel::provider eq EmailProviders.Mailjet)
        println("------------------------------- ${provider?.lastUsedAt}")
        // Sample for making a HTTP Client request
        /*
        val message = client.post<JsonSampleClass> {
            url("http://127.0.0.1:8080/path/to/endpoint")
            contentType(ContentType.Application.Json)
            body = JsonSampleClass(hello = "world")
        }
        */
    }

    install(Authentication) {
    }

    install(ContentNegotiation) {
        jackson {
        }
    }

    install(Routing) {
        trace { application.log.trace(it.buildText()) }
        emailEndpoints()
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