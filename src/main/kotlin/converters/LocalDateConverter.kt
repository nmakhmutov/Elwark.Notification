package com.elwark.notification.converters

import com.google.gson.*
import java.lang.reflect.Type
import java.time.LocalDate
import java.time.format.DateTimeFormatter

class LocalDateConverter : JsonSerializer<LocalDate>, JsonDeserializer<LocalDate> {
    override fun serialize(src: LocalDate?, typeOfSrc: Type?, context: JsonSerializationContext?): JsonElement? {
        return JsonPrimitive(DATEFORMATTER.format(src))
    }

    override fun deserialize(json: JsonElement?, typeOfT: Type?, context: JsonDeserializationContext?): LocalDate? {
        if (json == null)
            return null

        return try {
            LocalDate.parse(json.asString, DATEFORMATTER)
        } catch (e: Exception) {
            LocalDate.parse(json.asString, DATETIMEFORMATTER)
        }
    }

    companion object {
        private val DATEFORMATTER = DateTimeFormatter.ofPattern("yyyy-MM-dd");
        private val DATETIMEFORMATTER = DateTimeFormatter.ofPattern("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'")
    }
}