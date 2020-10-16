package com.elwark.notification.db

import com.elwark.notification.email.ProviderType
import com.elwark.notification.email.UpdateInterval
import kotlinx.coroutines.runBlocking
import org.litote.kmongo.coroutine.CoroutineCollection
import org.litote.kmongo.coroutine.coroutine
import org.litote.kmongo.reactivestreams.KMongo
import java.time.*

class MongoDbContext(connectionString: String, database: String) {
    val emailProviders: CoroutineCollection<ProviderModel>

    init {
        val mongoClient = KMongo.createClient(connectionString).coroutine
        val mongodb = mongoClient.getDatabase(database)

        emailProviders = mongodb.getCollection("email_providers")
        runBlocking {
            val count =emailProviders.countDocuments()
            if (count == 0L)
                seed()
        }
    }

    private suspend fun seed() {
        emailProviders.ensureUniqueIndex(ProviderModel::type)

        val tomorrow = LocalDateTime.of(LocalDate.now(ZoneOffset.UTC), LocalTime.MIDNIGHT).plusDays(1)
        emailProviders.insertMany(
            listOf(
                ProviderModel(
                    ProviderType.Sendgrid,
                    100,
                    100,
                    UpdateInterval.Daily,
                    tomorrow.plusSeconds(1)
                ),
                ProviderModel(
                    ProviderType.Sendinblue,
                    300,
                    300,
                    UpdateInterval.Daily,
                    tomorrow.plusSeconds(1)
                ),
                ProviderModel(
                    ProviderType.Gmail,
                    100,
                    100,
                    UpdateInterval.Daily,
                    tomorrow.plusSeconds(1)
                )
            )
        )
    }
}