import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import AvailabilityPage from "./AvailabilityPage";
import * as availabilityService from "../../services/availabilityService";
import * as authContext from "../../context/AuthContext";
import type { AvailabilityDto, DriverScheduleDto } from "../../types/availability";

vi.mock("../../services/availabilityService");
vi.mock("../../services/driverBookingService");
vi.mock("../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedAvailabilityService = vi.mocked(availabilityService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function renderPage() {
  return render(
    <MemoryRouter>
      <AvailabilityPage />
    </MemoryRouter>
  );
}

function makeAvailability(overrides: Partial<AvailabilityDto> = {}): AvailabilityDto {
  return {
    id: "11111111-1111-1111-1111-111111111111",
    driverId: "22222222-2222-2222-2222-222222222222",
    date: "2026-09-15",
    startTime: "08:00:00",
    endTime: "17:00:00",
    isAvailable: true,
    notes: null,
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-01T00:00:00Z",
    ...overrides,
  };
}

function schedule(items: AvailabilityDto[], isCurrentlyAvailable = false): DriverScheduleDto {
  return { isCurrentlyAvailable, schedule: items };
}

beforeEach(() => {
  vi.clearAllMocks();
  mockedUseAuth.mockReturnValue({
    user: { id: "u1", email: "driver@example.com", firstName: "John", lastName: "Smith", role: "Driver" },
    accessToken: "test-token",
    expiresAt: null,
    isAuthenticated: true,
    login: vi.fn(),
    logout: vi.fn(),
  });
});

describe("AvailabilityPage", () => {
  it("loads and displays current availability and the schedule", async () => {
    mockedAvailabilityService.getMySchedule.mockResolvedValue(schedule([makeAvailability()], true));

    renderPage();

    // "Available" appears both as the current-status text and in the
    // schedule table's Status column, so expect exactly those two matches.
    expect(await screen.findAllByText("Available")).toHaveLength(2);
    expect(screen.getByRole("button", { name: "Set Unavailable" })).toBeInTheDocument();
    expect(screen.getByText("08:00")).toBeInTheDocument();
    expect(screen.getByText("17:00")).toBeInTheDocument();
  });

  it("toggles current availability", async () => {
    mockedAvailabilityService.getMySchedule.mockResolvedValue(schedule([], false));
    mockedAvailabilityService.setCurrentAvailability.mockResolvedValue({ isAvailable: true });
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Availability Periods Yet");

    await user.click(screen.getByRole("button", { name: "Set Available" }));

    await waitFor(() => {
      expect(mockedAvailabilityService.setCurrentAvailability).toHaveBeenCalledWith(true, "test-token");
    });
    expect(await screen.findByRole("status")).toHaveTextContent("Current availability set to Available.");
  });

  it("shows validation errors for an incomplete add-availability form", async () => {
    mockedAvailabilityService.getMySchedule.mockResolvedValue(schedule([]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Availability Periods Yet");

    await user.click(screen.getByRole("button", { name: "Add Availability" }));
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByText("Date is required.")).toBeInTheDocument();
    expect(mockedAvailabilityService.createAvailability).not.toHaveBeenCalled();
  });

  it("creates a new availability period", async () => {
    mockedAvailabilityService.getMySchedule.mockResolvedValue(schedule([]));
    mockedAvailabilityService.createAvailability.mockResolvedValue(makeAvailability());
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Availability Periods Yet");

    await user.click(screen.getByRole("button", { name: "Add Availability" }));
    await user.type(screen.getByLabelText("Date"), "2026-09-15");
    await user.type(screen.getByLabelText("Start time"), "08:00");
    await user.type(screen.getByLabelText("End time"), "17:00");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() => {
      expect(mockedAvailabilityService.createAvailability).toHaveBeenCalledWith(
        expect.objectContaining({ date: "2026-09-15", startTime: "08:00", endTime: "17:00" }),
        "test-token"
      );
    });
    expect(await screen.findByRole("status")).toHaveTextContent("Availability created successfully.");
  });

  it("shows the backend's overlap-conflict error inside the form", async () => {
    mockedAvailabilityService.getMySchedule.mockResolvedValue(schedule([]));
    mockedAvailabilityService.createAvailability.mockRejectedValue(
      new Error("The driver already has an overlapping availability period for this date.")
    );
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Availability Periods Yet");

    await user.click(screen.getByRole("button", { name: "Add Availability" }));
    await user.type(screen.getByLabelText("Date"), "2026-09-15");
    await user.type(screen.getByLabelText("Start time"), "08:00");
    await user.type(screen.getByLabelText("End time"), "17:00");
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(
      await screen.findByText("The driver already has an overlapping availability period for this date.")
    ).toBeInTheDocument();
  });

  it("edits an existing availability period", async () => {
    const item = makeAvailability();
    mockedAvailabilityService.getMySchedule.mockResolvedValue(schedule([item]));
    mockedAvailabilityService.updateAvailability.mockResolvedValue({ ...item, endTime: "18:00:00" });
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("08:00");

    await user.click(screen.getByRole("button", { name: "Edit" }));
    const endTimeInput = screen.getByLabelText("End time");
    await user.clear(endTimeInput);
    await user.type(endTimeInput, "18:00");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() => {
      expect(mockedAvailabilityService.updateAvailability).toHaveBeenCalledWith(
        item.id,
        expect.objectContaining({ endTime: "18:00" }),
        "test-token"
      );
    });
    expect(await screen.findByRole("status")).toHaveTextContent("Availability updated successfully.");
  });

  it("removes an availability period after confirmation", async () => {
    const item = makeAvailability();
    mockedAvailabilityService.getMySchedule.mockResolvedValue(schedule([item]));
    vi.spyOn(window, "confirm").mockReturnValue(true);
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("08:00");

    await user.click(screen.getByRole("button", { name: "Remove" }));

    expect(window.confirm).toHaveBeenCalledWith("Are you sure you want to remove this availability period?");
    await waitFor(() => {
      expect(mockedAvailabilityService.deleteAvailability).toHaveBeenCalledWith(item.id, "test-token");
    });
  });

  it("does not remove an availability period when the confirmation is declined", async () => {
    const item = makeAvailability();
    mockedAvailabilityService.getMySchedule.mockResolvedValue(schedule([item]));
    vi.spyOn(window, "confirm").mockReturnValue(false);
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("08:00");

    await user.click(screen.getByRole("button", { name: "Remove" }));

    expect(mockedAvailabilityService.deleteAvailability).not.toHaveBeenCalled();
  });
});
