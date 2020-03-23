package com.elwark.notification.api

import com.elwark.notification.email.EmailProviders
import com.elwark.notification.email.ProviderBalanceResponse
import io.ktor.application.call
import io.ktor.http.HttpStatusCode
import io.ktor.response.respond
import io.ktor.routing.Routing
import io.ktor.routing.get
import io.ktor.routing.route
import org.slf4j.Logger
import org.slf4j.LoggerFactory
import java.time.LocalDateTime
import java.time.ZoneOffset

val balances = arrayOf(
    ProviderBalanceResponse(EmailProviders.Mailjet, 100, LocalDateTime.now(ZoneOffset.UTC)),
    ProviderBalanceResponse(EmailProviders.Sendgrid, 500, LocalDateTime.now(ZoneOffset.UTC))
)

fun Routing.emailEndpoints() {
    val logger: Logger = LoggerFactory.getLogger("EmailController")

    route("/balance") {
        get("/") {
            logger.debug("Message on root")
            call.respond(balances)
        }

        get("/{name}") {
            val name = call.parameters["name"]!!
            val value = EmailProviders.valueOf(name)

            call.respond(balances.firstOrNull { it.provider == value } ?: HttpStatusCode.NotFound)
        }
    }
}