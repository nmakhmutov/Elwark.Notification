package com.elwark.notification.api

import com.elwark.notification.email.EmailProviders
import com.elwark.notification.email.ProviderBalance
import io.ktor.application.call
import io.ktor.http.HttpStatusCode
import io.ktor.response.respond
import io.ktor.routing.Routing
import io.ktor.routing.get
import io.ktor.routing.route
import java.time.LocalDateTime
import java.time.ZoneOffset

val balances = arrayOf(
    ProviderBalance(EmailProviders.Mailjet, 100, LocalDateTime.now(ZoneOffset.UTC)),
    ProviderBalance(EmailProviders.Sendgrid, 500, LocalDateTime.now(ZoneOffset.UTC))
)

fun Routing.emailEndpoints() {
    route("/balance") {
        get("/") {
            call.respond(balances)
        }

        get("/{name}") {
            val name = call.parameters["name"]!!
            val value = EmailProviders.valueOf(name)

            call.respond(balances.firstOrNull { it.provider == value } ?: HttpStatusCode.NotFound)
        }
    }
}