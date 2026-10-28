import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import CrmTasksPage from "./CrmTasksPage";
import { ToastProvider } from "../../../../context/ToastContext";
import * as crmTaskService from "../../../../services/crmTaskService";
import * as administratorService from "../../../../services/administratorService";
import * as authContext from "../../../../context/AuthContext";
import type { CrmTaskDto } from "../../../../types/crmTask";
import type { PagedResult } from "../../../../types/api";

vi.mock("../../../../services/crmTaskService");
vi.mock("../../../../services/administratorService");
vi.mock("../../../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedTaskService = vi.mocked(crmTaskService);
const mockedAdministratorService = vi.mocked(administratorService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeTask(overrides: Partial<CrmTaskDto> = {}): CrmTaskDto {
  return {
    id: "task-1",
    title: "Send proposal",
    description: null,
    dueDate: "2026-11-01",
    dueTime: null,
    assignedUser: { id: "u1", firstName: "Admin", lastName: "User", email: "admin@example.com" },
    priority: "High",
    status: "ToDo",
    companyId: null,
    companyName: null,
    contactId: null,
    contactName: null,
    dealId: "deal-1",
    dealName: "Annual chauffeur contract",
    completedAt: null,
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-01T00:00:00Z",
    ...overrides,
  };
}

function pagedResult<T>(items: T[]): PagedResult<T> {
  return { items, page: 1, pageSize: 25, totalCount: items.length, totalPages: 1 };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <ToastProvider>
        <CrmTasksPage />
      </ToastProvider>
    </MemoryRouter>
  );
}

beforeEach(() => {
  vi.clearAllMocks();
  mockedUseAuth.mockReturnValue({
    user: { id: "u1", email: "admin@example.com", firstName: "Admin", lastName: "User", role: "Administrator", languageCode: null },
    accessToken: "test-token",
    expiresAt: null,
    isAuthenticated: true,
    login: vi.fn(),
    logout: vi.fn(),
  });
  mockedAdministratorService.getAdministrators.mockResolvedValue([
    { id: "u1", firstName: "Admin", lastName: "User", email: "admin@example.com" },
  ]);
});

describe("CrmTasksPage", () => {
  it("renders the task list", async () => {
    mockedTaskService.getCrmTasks.mockResolvedValue(pagedResult([makeTask()]));

    renderPage();

    expect(await screen.findByText("Send proposal")).toBeInTheDocument();
    expect(screen.getByText("Annual chauffeur contract")).toBeInTheDocument();
  });

  it("switches to the My Tasks tab and filters by the current user", async () => {
    mockedTaskService.getCrmTasks.mockResolvedValue(pagedResult([makeTask()]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("Send proposal");

    await user.click(screen.getByRole("button", { name: "My Tasks" }));

    await waitFor(() => {
      expect(mockedTaskService.getCrmTasks).toHaveBeenLastCalledWith(
        expect.objectContaining({ assignedUserId: "u1" }),
        "test-token"
      );
    });
  });

  it("switches to the Overdue tab", async () => {
    mockedTaskService.getCrmTasks.mockResolvedValue(pagedResult([makeTask()]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("Send proposal");

    await user.click(screen.getByRole("button", { name: "Overdue" }));

    await waitFor(() => {
      expect(mockedTaskService.getCrmTasks).toHaveBeenLastCalledWith(expect.objectContaining({ overdue: true }), "test-token");
    });
  });

  it("completes a task", async () => {
    const task = makeTask();
    mockedTaskService.getCrmTasks.mockResolvedValue(pagedResult([task]));
    mockedTaskService.completeCrmTask.mockResolvedValue({ ...task, status: "Completed" });
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("Send proposal");

    await user.click(screen.getByRole("button", { name: "Complete" }));

    await waitFor(() => {
      expect(mockedTaskService.completeCrmTask).toHaveBeenCalledWith(task.id, "test-token");
    });
    expect(await screen.findByText("Task completed.")).toBeInTheDocument();
  });

  it("rejects an empty create-task form client-side", async () => {
    mockedTaskService.getCrmTasks.mockResolvedValue(pagedResult([]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Tasks Found");

    await user.click(screen.getByRole("button", { name: "+ New Task" }));
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByText("Title is required.")).toBeInTheDocument();
    expect(mockedTaskService.createCrmTask).not.toHaveBeenCalled();
  });

  it("creates a task", async () => {
    mockedTaskService.getCrmTasks.mockResolvedValue(pagedResult([]));
    mockedTaskService.createCrmTask.mockResolvedValue(makeTask());
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Tasks Found");

    await user.click(screen.getByRole("button", { name: "+ New Task" }));
    await user.type(screen.getByLabelText("Title"), "Send proposal");
    await user.type(screen.getByLabelText("Due date"), "2026-11-01");
    await user.selectOptions(screen.getByLabelText("Assigned to"), "u1");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() => {
      expect(mockedTaskService.createCrmTask).toHaveBeenCalledWith(
        expect.objectContaining({ title: "Send proposal", dueDate: "2026-11-01", assignedUserId: "u1" }),
        "test-token"
      );
    });
    expect(await screen.findByText("Task created successfully.")).toBeInTheDocument();
  });
});
