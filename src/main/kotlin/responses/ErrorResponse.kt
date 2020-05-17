package com.elwark.notification.responses

data class ErrorResponse(val title: String, val message: String, val errors: Map<String, List<String>>? = null)