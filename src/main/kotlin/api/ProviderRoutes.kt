package com.elwark.notification.api

import com.elwark.notification.email.ProviderType
import com.elwark.notification.email.EmailBalanceService
import io.ktor.application.call
import io.ktor.http.HttpStatusCode
import io.ktor.request.receive
import io.ktor.response.respond
import io.ktor.routing.Routing
import io.ktor.routing.*
import io.ktor.routing.route
import org.valiktor.functions.*
import org.valiktor.validate
import java.time.LocalDate

data class Post(val limit: Int, val interval: Long, val updateAt: LocalDate) {
    companion object  {
        fun validate(post: Post) = validate(post) {
            validate(Post::limit).isGreaterThanOrEqualTo(0)
            validate(Post::interval).isGreaterThan(0L)
            validate(Post::updateAt).isNotNull()
        }
    }
}

fun Routing.providersEndpoints(emailService: EmailBalanceService) {
    //authenticate {
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
            Post.validate(request)
            emailService.update(provider, request.limit, request.interval, request.updateAt)

            call.respond(HttpStatusCode.OK)
        }
        //}
    }
}