const now = new Date();
const reservationPublicId = "C3000000-0000-4000-8000-000000000001";
const userPublicId = "B2000000-0000-4000-8000-000000000001";
const eventPublicId = "A1000000-0000-4000-8000-000000000001";
const ticketPublicId = "D4000000-0000-4000-8000-000000000001";
const reviewId = "E5000000-0000-4000-8000-000000000001";
const discussionId = "F6000000-0000-4000-8000-000000000001";

db.payments.updateOne(
  { _id: "demo-payment-succeeded" },
  {
    $setOnInsert: {
      reservationPublicId,
      userPublicId,
      eventPublicId,
      eventTitleSnapshot: "Demo Concert",
      amount: NumberDecimal("500.00"),
      currency: "UAH",
      status: "SUCCEEDED",
      provider: "demo",
      providerPaymentId: "demo-provider-payment-001",
      createdAt: now,
      updatedAt: now
    }
  },
  { upsert: true }
);

db.payments.updateOne(
  { _id: "demo-payment-pending" },
  {
    $setOnInsert: {
      reservationPublicId: "C3000000-0000-4000-8000-000000000002",
      userPublicId,
      amount: NumberDecimal("1200.00"),
      currency: "UAH",
      status: "PENDING",
      createdAt: now
    }
  },
  { upsert: true }
);

db.notifications.updateOne(
  { _id: "demo-notification-ticket" },
  {
    $setOnInsert: {
      userPublicId,
      type: "TICKET",
      title: "Your ticket is ready",
      message: "Open your wallet to view the ticket QR code.",
      event: {
        eventPublicId,
        title: "Demo Concert",
        startsAt: ISODate("2099-06-15T15:00:00Z")
      },
      readAt: null,
      createdAt: now
    }
  },
  { upsert: true }
);

db.reviews.updateOne(
  { _id: reviewId },
  {
    $setOnInsert: {
      eventPublicId,
      eventTitleSnapshot: "Demo Concert",
      ticketPublicId,
      userPublicId,
      author: {
        displayName: "Demo Attendee",
        avatarUrl: "https://example.invalid/avatars/demo.png"
      },
      rating: NumberInt(5),
      text: "Great live music and a welcoming venue.",
      photos: ["https://example.invalid/events/demo/stage.jpg"],
      comments: [
        {
          author: "Event Organizer",
          text: "Thank you for coming!",
          createdAt: now
        }
      ],
      discussionId,
      createdAt: now
    }
  },
  { upsert: true }
);

db.reviews.updateOne(
  { _id: "E5000000-0000-4000-8000-000000000002" },
  {
    $setOnInsert: {
      eventPublicId,
      eventTitleSnapshot: "Demo Concert",
      ticketPublicId: "D4000000-0000-4000-8000-000000000002",
      userPublicId: "B2000000-0000-4000-8000-000000000002",
      author: { displayName: "Another Attendee" },
      rating: NumberInt(4),
      text: "Excellent performance.",
      comments: [],
      createdAt: now
    }
  },
  { upsert: true }
);

db.discussions.updateOne(
  { _id: "demo-discussion-message-001" },
  {
    $setOnInsert: {
      reviewId,
      discussionId,
      author: { userPublicId, displayName: "Demo Attendee" },
      text: "Does the venue have accessible seating?",
      createdAt: now
    }
  },
  { upsert: true }
);

db.discussions.updateOne(
  { _id: "demo-discussion-message-002" },
  {
    $setOnInsert: {
      reviewId,
      discussionId,
      author: {
        userPublicId: "A7000000-0000-4000-8000-000000000001",
        displayName: "Event Organizer"
      },
      text: "Yes, please contact our team before the event.",
      attachments: ["https://example.invalid/venue/accessibility-map.png"],
      createdAt: new Date(now.getTime() + 1000)
    }
  },
  { upsert: true }
);
