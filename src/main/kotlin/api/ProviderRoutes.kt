package com.elwark.notification.api

import com.elwark.notification.email.ProviderType
import com.elwark.notification.email.EmailBalanceService
import io.ktor.application.call
import io.ktor.auth.authenticate
import io.ktor.http.HttpStatusCode
import io.ktor.request.receive
import io.ktor.response.respond
import io.ktor.routing.*

data class Post(val dailyLimit: Int)

fun Routing.providersEndpoints(emailService: EmailBalanceService) {
    authenticate {
        route("/providers") {

            get("/") {
                val data = emailService.getAll()
                call.respond(data)
            }

            put("/{provider}") {
                val provider = ProviderType.values()
                    .firstOrNull { it.toString().equals((call.parameters["provider"]!!).toLowerCase(), true) }
                    ?: return@put call.respond(HttpStatusCode.NotFound)

                val request = call.receive<Post>()
                emailService.updateDailyLimit(provider, request.dailyLimit)

                call.respond(HttpStatusCode.OK)
            }
        }
    }
}