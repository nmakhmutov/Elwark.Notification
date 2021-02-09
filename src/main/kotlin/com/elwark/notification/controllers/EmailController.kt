package com.elwark.notification.controllers

import com.elwark.notification.events.EmailEvent
import com.elwark.notification.events.senders.EmailEventSender
import org.springframework.http.HttpStatus
import org.springframework.http.ResponseEntity
import org.springframework.web.bind.annotation.PostMapping
import org.springframework.web.bind.annotation.RequestBody
import org.springframework.web.bind.annotation.RequestMapping
import org.springframework.web.bind.annotation.RestController
import javax.validation.Valid
import javax.validation.constraints.Email
import javax.validation.constraints.NotEmpty

@RestController
@RequestMapping("/email")
class EmailController(val emailSender: EmailEventSender) {

    @PostMapping
    fun get(@Valid @RequestBody request: SendEmailRequest) :ResponseEntity<Any> {
        emailSender.send(EmailEvent(request.email, request.subject, request.body))

        return ResponseEntity(HttpStatus.ACCEPTED)
    }
}

data class SendEmailRequest(
    @field:NotEmpty
    @field:Email
    val email: String,

    @field:NotEmpty
    val subject: String,

    @field:NotEmpty
    val body: String
)