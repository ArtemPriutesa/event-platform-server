const definitions = {
  payments: {
    required: [
      "reservationPublicId",
      "userPublicId",
      "amount",
      "currency",
      "status",
      "createdAt"
    ],
    properties: {
      reservationPublicId: { bsonType: "string", minLength: 1 },
      userPublicId: { bsonType: "string", minLength: 1 },
      amount: { bsonType: "decimal", minimum: 0 },
      currency: { bsonType: "string", pattern: "^[A-Z]{3}$" },
      status: {
        enum: ["PENDING", "SUCCEEDED", "FAILED", "REFUNDED"]
      },
      provider: { bsonType: "string" },
      providerPaymentId: { bsonType: "string" },
      eventPublicId: { bsonType: "string" },
      eventTitleSnapshot: { bsonType: "string" },
      createdAt: { bsonType: "date" },
      updatedAt: { bsonType: "date" },
      refundedAt: { bsonType: "date" }
    }
  },
  notifications: {
    required: ["userPublicId", "type", "title", "message", "createdAt"],
    properties: {
      userPublicId: { bsonType: "string", minLength: 1 },
      type: { enum: ["PAYMENT", "EVENT", "TICKET", "SYSTEM"] },
      title: { bsonType: "string", minLength: 1 },
      message: { bsonType: "string", minLength: 1 },
      readAt: { bsonType: ["date", "null"] },
      createdAt: { bsonType: "date" },
      event: {
        bsonType: "object",
        required: ["eventPublicId", "title"],
        properties: {
          eventPublicId: { bsonType: "string" },
          title: { bsonType: "string" },
          startsAt: { bsonType: "date" }
        }
      }
    }
  },
  reviews: {
    required: [
      "eventPublicId",
      "eventTitleSnapshot",
      "ticketPublicId",
      "userPublicId",
      "author",
      "rating",
      "text",
      "createdAt"
    ],
    properties: {
      eventPublicId: { bsonType: "string", minLength: 1 },
      eventTitleSnapshot: { bsonType: "string", minLength: 1 },
      ticketPublicId: { bsonType: "string", minLength: 1 },
      userPublicId: { bsonType: "string", minLength: 1 },
      author: {
        bsonType: "object",
        required: ["displayName"],
        properties: {
          displayName: { bsonType: "string", minLength: 1 },
          avatarUrl: { bsonType: "string" }
        }
      },
      rating: { bsonType: "int", minimum: 1, maximum: 5 },
      text: { bsonType: "string" },
      photos: { bsonType: "array", items: { bsonType: "string" } },
      comments: {
        bsonType: "array",
        items: {
          bsonType: "object",
          required: ["author", "text", "createdAt"],
          properties: {
            author: { bsonType: "string", minLength: 1 },
            text: { bsonType: "string", minLength: 1 },
            createdAt: { bsonType: "date" }
          }
        }
      },
      discussionId: { bsonType: "string" },
      createdAt: { bsonType: "date" },
      updatedAt: { bsonType: "date" }
    }
  },
  discussions: {
    required: ["reviewId", "discussionId", "author", "text", "createdAt"],
    properties: {
      reviewId: { bsonType: "string", minLength: 1 },
      discussionId: { bsonType: "string", minLength: 1 },
      author: {
        bsonType: "object",
        required: ["userPublicId", "displayName"],
        properties: {
          userPublicId: { bsonType: "string" },
          displayName: { bsonType: "string" }
        }
      },
      text: { bsonType: "string", minLength: 1 },
      attachments: { bsonType: "array", items: { bsonType: "string" } },
      createdAt: { bsonType: "date" }
    }
  }
};

for (const [name, definition] of Object.entries(definitions)) {
  const validator = {
    $jsonSchema: {
      bsonType: "object",
      required: definition.required,
      properties: definition.properties
    }
  };

  if (!db.getCollectionNames().includes(name)) {
    db.createCollection(name, {
      validator,
      validationLevel: "strict",
      validationAction: "error"
    });
  } else {
    db.runCommand({
      collMod: name,
      validator,
      validationLevel: "strict",
      validationAction: "error"
    });
  }
}

const obsoleteIndexes = {
  payments: ["reservationId_1"],
  reviews: ["eventId_1_userId_1", "ticketId_1"],
  notifications: ["userId_1_createdAt_-1"]
};

for (const [collectionName, indexNames] of Object.entries(obsoleteIndexes)) {
  const existingIndexes = db[collectionName].getIndexes().map(index => index.name);
  for (const indexName of indexNames) {
    if (existingIndexes.includes(indexName)) {
      db[collectionName].dropIndex(indexName);
    }
  }
}

db.payments.createIndex({ reservationPublicId: 1 }, { unique: true });
db.payments.createIndex({ userPublicId: 1, createdAt: -1 });
db.notifications.createIndex({ userPublicId: 1, createdAt: -1 });
db.reviews.createIndex({ eventPublicId: 1, createdAt: -1 });
db.reviews.createIndex({ ticketPublicId: 1 }, { unique: true });
db.reviews.createIndex({ userPublicId: 1, eventPublicId: 1 }, { unique: true });
db.discussions.createIndex({ discussionId: 1, createdAt: 1 });
db.discussions.createIndex({ reviewId: 1, createdAt: 1 });
