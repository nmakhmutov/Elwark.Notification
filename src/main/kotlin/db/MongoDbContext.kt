package com.elwark.notification.db

import org.litote.kmongo.coroutine.CoroutineCollection
import org.litote.kmongo.coroutine.coroutine
import org.litote.kmongo.reactivestreams.KMongo

class MongoDbContext(connectionString: String, database: String) {
    val emailProviders: CoroutineCollection<ProviderModel>

    init {
        val mongoClient = KMongo.createClient(connectionString).coroutine
        val mongodb = mongoClient.getDatabase(database)

        emailProviders = mongodb.getCollection("email_providers")
    }

}