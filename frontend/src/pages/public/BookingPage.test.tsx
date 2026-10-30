import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import BookingPage from "./BookingPage";
import * as bookingService from "../../services/bookingService";
import * as paymentService from "../../services/paymentService";
import { ApiError } from "../../services/apiClient";
import { AuthProvider } from "../../context/AuthContext";
import i18n, { DEFAULT_LANGUAGE } from "../../i18n/i18n";
import type { BookingDto, PublicRouteDto } from "../../types/booking";

vi.mock("../../services/bookingService");
vi.mock("../../services/paymentService");

// fillStep1 opens the lazy-loaded date picker (its own dynamic import) on
// top of walking several form steps — comfortably fast alone, but the two
// can combine to exceed the 5s default under heavy parallel test-suite load.
vi.setConfig({ testTimeout: 15000 });

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
      <AuthProvider>
        <BookingPage />
      </AuthProvider>
    </MemoryRouter>
  );
}

async function fillStep1(user: ReturnType<typeof userEvent.setup>) {
  await user.click(screen.getByRole("radio", { name: /Basel.*Zurich/s }));

  await user.click(screen.getByRole("button", { name: "Booking date" }));
  const dialog = screen.getByRole("dialog", { name: "Date of travel" });
  await user.click(await within(dialog).findByRole("button", { name: "Go to the Next Month" }, { timeout: 3000 }));
  await user.click(await within(dialog).findByRole("button", { name: /15th/ }, { timeout: 3000 }));
  await user.type(within(dialog).getByLabelText("Pickup time"), "14:30");
  await user.click(within(dialog).getByRole("button", { name: "Apply" }));

  await user.click(screen.getByRole("button", { name: "Next" }));
}

/** Fills the merged "Details" step (pickup details + customer info are one
 * screen now, not two) and advances to Review. */
async function fillStep2(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText("Pickup address"), "Bahnhofplatz 1, Basel");
  await user.type(screen.getByLabelText("First name"), "Jane");
  await user.type(screen.getByLabelText("Last name"), "Doe");
  await user.type(screen.getByLabelText("Email"), "jane.doe@example.com");
  await user.type(screen.getByLabelText("Phone"), "+41791234567");
  await user.click(screen.getByRole("button", { name: "Next" }));
}

beforeEach(() => {
  vi.clearAllMocks();
  sessionStorage.clear();
  mockedBookingService.getActiveRoutes.mockResolvedValue([makeRoute()]);
});

afterEach(async () => {
  await i18n.changeLanguage(DEFAULT_LANGUAGE);
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

  it("rejects an invalid email on the details step", async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByLabelText("Route");

    await fillStep1(user);
    await user.type(screen.getByLabelText("Pickup address"), "Bahnhofplatz 1, Basel");
    await user.type(screen.getByLabelText("First name"), "Jane");
    await user.type(screen.getByLabelText("Last name"), "Doe");
    await user.type(screen.getByLabelText("Email"), "not-an-email");
    await user.type(screen.getByLabelText("Phone"), "+41791234567");
    await user.click(screen.getByRole("button", { name: "Next" }));

    expect(await screen.findByText("Enter a valid email address.")).toBeInTheDocument();
    expect(mockedBookingService.createBooking).not.toHaveBeenCalled();
  });

  it("clears a field's error as soon as it's corrected, without waiting for the next Next click", async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByLabelText("Route");

    await user.click(screen.getByRole("button", { name: "Next" }));
    expect(await screen.findByText("Please select a route.")).toBeInTheDocument();

    await user.click(screen.getByRole("radio", { name: /Basel.*Zurich/s }));
    expect(screen.queryByText("Please select a route.")).not.toBeInTheDocument();
  });

  it("walks through all steps and shows a review summary before submitting", async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByLabelText("Route");

    await fillStep1(user);
    await fillStep2(user);

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
    await user.click(screen.getByRole("button", { name: "Confirm Booking" }));

    await screen.findByText("Booking Confirmed");
    expect(screen.queryByText(/driver/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/vehicle/i)).not.toBeInTheDocument();
  });

  it("preserves entered booking details when the language is switched mid-flow", async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByLabelText("Route");

    await fillStep1(user);
    await user.type(screen.getByLabelText("Pickup address"), "Bahnhofplatz 1, Basel");

    await user.click(screen.getByRole("button", { name: "Select language" }));
    await user.click(screen.getByRole("menuitemradio", { name: "DE" }));
    await screen.findByLabelText("Abholadresse");

    // The pickup-address value survives the language switch even though the
    // field's own label just changed language (section 40).
    expect(screen.getByLabelText("Abholadresse")).toHaveValue("Bahnhofplatz 1, Basel");
  });

  it("shows the server error and stays on the review step when submission fails", async () => {
    mockedBookingService.createBooking.mockRejectedValue(new ApiError(400, "Bookings require at least 120 minutes of lead time."));
    const user = userEvent.setup();
    renderPage();
    await screen.findByLabelText("Route");

    await fillStep1(user);
    await fillStep2(user);
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
    await user.click(screen.getByRole("button", { name: "Confirm Booking" }));
    await screen.findByText("Booking Confirmed");

    await user.click(screen.getByRole("button", { name: "Pay Now" }));

    expect(await screen.findByText("This booking has already been paid.")).toBeInTheDocument();
  });
});
