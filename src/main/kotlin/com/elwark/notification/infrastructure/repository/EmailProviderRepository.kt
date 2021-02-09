package com.elwark.notification.infrastructure.repository

import com.elwark.notification.models.EmailProvider
import org.bson.types.ObjectId
import org.springframework.data.mongodb.repository.MongoRepository
import org.springframework.data.mongodb.repository.ReactiveMongoRepository
import org.springframework.stereotype.Repository

@Repository
interface EmailProviderRepository : MongoRepository<EmailProvider, ObjectId>