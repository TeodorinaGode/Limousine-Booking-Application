import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import BookingPage from "./BookingPage";
import * as bookingService from "../../services/bookingService";
import * as paymentService from "../../services/paymentService";
import { ApiError } from "../../services/apiClient";
import type { BookingDto, PublicRouteDto } from "../../types/booking";

vi.mock("../../services/bookingService");
vi.mock("../../services/paymentService");

const mockedBookingService = vi.mocked(bookingService);
const mockedPaymentService = vi.mocked(paymentService);

function makeRoute(overrides: Partial<PublicRouteDto> = {}): PublicRouteDto {
  return {
    id: "11111111-1111-1111-1111-111111111111",
    departureLocation: "Basel",
    destination: "Zurich",
    estimatedDurationMinutes: 60,
    price: 180,
    currency: "CHF",
    ...overrides,
  };
}

function makeBooking(overrides: Partial<BookingDto> = {}): BookingDto {
  return {
    id: "b1",
    bookingReference: "LM-20261225-123456",
    status: "Pending",
    route: { departureLocation: "Basel", destination: "Zurich" },
    bookingDate: "2026-12-25",
    pickupTime: "14:30:00",
    pickupAddress: "Bahnhofplatz 1, Basel",
    passengerCount: 2,
    notes: null,
    price: 180,
    currency: "CHF",
    accessToken: "test-access-token",
    ...overrides,
  };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <BookingPage />
    </MemoryRouter>
  );
}

const FUTURE_DATE = "2099-01-01";

async function fillStep1(user: ReturnType<typeof userEvent.setup>) {
  await user.click(screen.getByRole("radio", { name: /Basel.*Zurich/s }));
  const dateInput = screen.getByLabelText("Booking date");
  await user.clear(dateInput);
  await user.type(dateInput, FUTURE_DATE);
  const timeInput = screen.getByLabelText("Pickup time");
  await user.clear(timeInput);
  await user.type(timeInput, "14:30");
  await user.click(screen.getByRole("button", { name: "Next" }));
}

async function fillStep2(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText("Pickup address"), "Bahnhofplatz 1, Basel");
  await user.click(screen.getByRole("button", { name: "Next" }));
}

async function fillStep3(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText("First name"), "Jane");
  await user.type(screen.getByLabelText("Last name"), "Doe");
  await user.type(screen.getByLabelText("Email"), "jane.doe@example.com");
  await user.type(screen.getByLabelText("Phone"), "+41791234567");
  await user.click(screen.getByRole("button", { name: "Next" }));
}

beforeEach(() => {
  vi.clearAllMocks();
  mockedBookingService.getActiveRoutes.mockResolvedValue([makeRoute()]);
});

describe("BookingPage", () => {
  it("loads and lists active routes", async () => {
    renderPage();

    await waitFor(() => expect(mockedBookingService.getActiveRoutes).toHaveBeenCalled());
    expect(await screen.findByRole("radio", { name: /Basel.*Zurich/s })).toBeInTheDocument();
  });

  it("shows an error when routes fail to load", async () => {
    mockedBookingService.getActiveRoutes.mockRejectedValue(new Error("Network error"));

    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("Network error");
  });

  it("blocks advancing past step 1 without a route, date, and time", async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByLabelText("Route");

    await user.click(screen.getByRole("button", { name: "Next" }));

    expect(await screen.findByText("Please select a route.")).toBeInTheDocument();
    expect(screen.getByText("Booking date is required.")).toBeInTheDocument();
    expect(screen.getByText("Pickup time is required.")).toBeInTheDocument();
  });

  it("rejects an invalid email on the customer info step", async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByLabelText("Route");

    await fillStep1(user);
    await fillStep2(user);
    await user.type(screen.getByLabelText("First name"), "Jane");
    await user.type(screen.getByLabelText("Last name"), "Doe");
    await user.type(screen.getByLabelText("Email"), "not-an-email");
    await user.type(screen.getByLabelText("Phone"), "+41791234567");
    await user.click(screen.getByRole("button", { name: "Next" }));

    expect(await screen.findByText("Enter a valid email address.")).toBeInTheDocument();
    expect(mockedBookingService.createBooking).not.toHaveBeenCalled();
  });

  it("walks through all steps and shows a review summary before submitting", async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByLabelText("Route");

    await fillStep1(user);
    await fillStep2(user);
    await fillStep3(user);

    expect(await screen.findByText("Review & Confirm", { exact: false })).toBeInTheDocument();
    expect(screen.getByText("jane.doe@example.com")).toBeInTheDocument();
    expect(mockedBookingService.createBooking).not.toHaveBeenCalled();
  });

  it("shows a confirmed message when automatic assignment succeeds", async () => {
    mockedBookingService.createBooking.mockResolvedValue(makeBooking({ status: "Confirmed" }));
    const user = userEvent.setup();
    renderPage();
    await screen.findByLabelText("Route");

    await fillStep1(user);
    await fillStep2(user);
    await fillStep3(user);
    await user.click(screen.getByRole("button", { name: "Confirm Booking" }));

    expect(await screen.findByText("Booking Confirmed")).toBeInTheDocument();
    expect(screen.getByText("Your booking has been confirmed.")).toBeInTheDocument();
    expect(screen.getByText("LM-20261225-123456")).toBeInTheDocument();
    expect(mockedBookingService.createBooking).toHaveBeenCalledWith(
      expect.objectContaining({
        routeId: "11111111-1111-1111-1111-111111111111",
        customerEmail: "jane.doe@example.com",
      })
    );
  });

  it("shows an awaiting-confirmation message when automatic assignment could not find a driver", async () => {
    mockedBookingService.createBooking.mockResolvedValue(makeBooking({ status: "Pending" }));
    const user = userEvent.setup();
    renderPage();
    await screen.findByLabelText("Route");

    await fillStep1(user);
    await fillStep2(user);
    await fillStep3(user);
    await user.click(screen.getByRole("button", { name: "Confirm Booking" }));

    expect(await screen.findByText("Booking Received")).toBeInTheDocument();
    expect(screen.getByText("Your booking request has been received and is awaiting confirmation.")).toBeInTheDocument();
    expect(screen.getByText("LM-20261225-123456")).toBeInTheDocument();
  });

  it("never displays driver or vehicle information on the confirmation view", async () => {
    mockedBookingService.createBooking.mockResolvedValue(makeBooking({ status: "Confirmed" }));
    const user = userEvent.setup();
    renderPage();
    await screen.findByLabelText("Route");

    await fillStep1(user);
    await fillStep2(user);
    await fillStep3(user);
    await user.click(screen.getByRole("button", { name: "Confirm Booking" }));

    await screen.findByText("Booking Confirmed");
    expect(screen.queryByText(/driver/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/vehicle/i)).not.toBeInTheDocument();
  });

  it("shows the server error and stays on the review step when submission fails", async () => {
    mockedBookingService.createBooking.mockRejectedValue(new ApiError(400, "Bookings require at least 120 minutes of lead time."));
    const user = userEvent.setup();
    renderPage();
    await screen.findByLabelText("Route");

    await fillStep1(user);
    await fillStep2(user);
    await fillStep3(user);
    await user.click(screen.getByRole("button", { name: "Confirm Booking" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Bookings require at least 120 minutes of lead time.");
    expect(screen.queryByText("Booking Confirmed")).not.toBeInTheDocument();
  });

  it("starts a payment attempt when Pay Now is clicked", async () => {
    mockedBookingService.createBooking.mockResolvedValue(makeBooking({ status: "Confirmed", accessToken: "abc-token" }));
    mockedPaymentService.createPayment.mockResolvedValue({
      paymentId: "p1",
      checkoutUrl: "https://checkout.example/cs_test_1",
      expiresAt: "2026-12-25T15:00:00Z",
    });
    const user = userEvent.setup();
    renderPage();
    await screen.findByLabelText("Route");

    await fillStep1(user);
    await fillStep2(user);
    await fillStep3(user);
    await user.click(screen.getByRole("button", { name: "Confirm Booking" }));
    await screen.findByText("Booking Confirmed");

    await user.click(screen.getByRole("button", { name: "Pay Now" }));

    await waitFor(() => expect(mockedPaymentService.createPayment).toHaveBeenCalledWith("LM-20261225-123456", "abc-token"));
  });

  it("shows an error when starting payment fails", async () => {
    mockedBookingService.createBooking.mockResolvedValue(makeBooking({ status: "Confirmed" }));
    mockedPaymentService.createPayment.mockRejectedValue(new ApiError(409, "This booking has already been paid."));
    const user = userEvent.setup();
    renderPage();
    await screen.findByLabelText("Route");

    await fillStep1(user);
    await fillStep2(user);
    await fillStep3(user);
    await user.click(screen.getByRole("button", { name: "Confirm Booking" }));
    await screen.findByText("Booking Confirmed");

    await user.click(screen.getByRole("button", { name: "Pay Now" }));

    expect(await screen.findByText("This booking has already been paid.")).toBeInTheDocument();
  });
});
