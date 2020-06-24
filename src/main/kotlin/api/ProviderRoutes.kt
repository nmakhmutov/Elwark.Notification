package com.elwark.notification.api

import com.elwark.notification.email.ProviderType
import com.elwark.notification.email.EmailBalanceService
import com.elwark.notification.email.UpdateInterval
import io.ktor.application.call
import io.ktor.auth.authenticate
import io.ktor.http.HttpStatusCode
import io.ktor.request.receive
import io.ktor.response.respond
import io.ktor.routing.Routing
import io.ktor.routing.*
import io.ktor.routing.route
import org.valiktor.functions.*
import org.valiktor.validate
import java.time.LocalDate

data class Post(val limit: Int, val updateInterval: UpdateInterval, val updateAt: LocalDate) {
    companion object {
        fun validate(post: Post) = validate(post) {
            validate(Post::limit).isGreaterThanOrEqualTo(0)
            validate(Post::updateInterval).isNotNull()
            validate(Post::updateAt).isNotNull()
        }
    }
}

fun Routing.providersEndpoints(emailService: EmailBalanceService) {
    authenticate {
        route("/providers") {

            get("/") {
                emailService.getAll().run { call.respond(it) }
            }

            put("/{provider}") {
                val provider = ProviderType.values()
                    .firstOrNull { it.toString().equals((call.parameters["provider"]!!).toLowerCase(), true) }
                    ?: return@put call.respond(HttpStatusCode.NotFound)

                val request = call.receive<Post>().also { Post.validate(it) }
                emailService.update(provider, request.limit, request.updateInterval, request.updateAt)
                emailService.get(provider).run { call.respond(it) }
            }
        }
    }
}