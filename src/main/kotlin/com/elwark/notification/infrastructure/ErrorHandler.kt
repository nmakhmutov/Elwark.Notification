package com.elwark.notification.infrastructure

import org.springframework.http.ResponseEntity
import org.springframework.web.bind.annotation.ControllerAdvice
import org.springframework.web.bind.annotation.ExceptionHandler
import org.springframework.web.bind.support.WebExchangeBindException
import java.lang.Exception

data class ErrorResponse(val type: String, val message: String)

data class ModelStateError(val type: String, val message: String, val errors: Map<String, List<String?>>)

@ControllerAdvice
class ErrorHandler {
    @ExceptionHandler(WebExchangeBindException::class)
    fun handler(ex: WebExchangeBindException): ResponseEntity<ModelStateError> {
        val errors = ex.bindingResult.fieldErrors.groupBy { x -> x.field }
            .map { x -> x.key to x.value.map { it.defaultMessage } }
            .toMap()

        return ResponseEntity.badRequest()
            .body(ModelStateError(ex::class.simpleName ?: "Unknown", ex.message, errors))
    }

    @ExceptionHandler(Exception::class)
    fun handler(ex: Exception): ResponseEntity<ErrorResponse> = ResponseEntity.badRequest()
        .body(ErrorResponse(ex::class.simpleName ?: "Unknown", ex.message ?: "Unknown error"))
}