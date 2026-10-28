import { useState, type FormEvent } from "react";
import Modal from "./Modal";
import { CRM_TASK_PRIORITIES, type CrmTaskDto } from "../types/crmTask";
import type { AdministratorSummaryDto } from "../types/administrator";

export interface TaskFormValues {
  title: string;
  description: string;
  dueDate: string;
  dueTime: string;
  assignedUserId: string;
  priority: string;
}

interface TaskFormModalProps {
  task?: CrmTaskDto;
  administrators: AdministratorSummaryDto[];
  /** Pre-fills whichever association this modal was opened from, when creating a new task. */
  companyId?: string;
  contactId?: string;
  dealId?: string;
  onSave: (values: TaskFormValues) => Promise<void>;
  onClose: () => void;
}

function toFormValues(task?: CrmTaskDto): TaskFormValues {
  return {
    title: task?.title ?? "",
    description: task?.description ?? "",
    dueDate: task?.dueDate ?? "",
    dueTime: task?.dueTime?.slice(0, 5) ?? "",
    assignedUserId: task?.assignedUser.id ?? "",
    priority: task?.priority ?? "Normal",
  };
}

function validate(values: TaskFormValues): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!values.title.trim()) errors.title = "Title is required.";
  if (!values.dueDate) errors.dueDate = "Due date is required.";
  if (!values.assignedUserId) errors.assignedUserId = "Assigned user is required.";
  return errors;
}

/** Launchable from the Tasks page or from any Company/Contact/Deal detail page, pre-associating whichever record it was opened from. */
function TaskFormModal({ task, administrators, onSave, onClose }: TaskFormModalProps) {
  const [values, setValues] = useState<TaskFormValues>(toFormValues(task));
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSaving, setIsSaving] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const isEdit = Boolean(task);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const validationErrors = validate(values);
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setSubmitError(null);
    setIsSaving(true);
    try {
      await onSave(values);
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : "Failed to save the task.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal title={isEdit ? "Edit Task" : "Add Task"} onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate>
        <div>
          <label htmlFor="crm-task-title">Title</label>
          <br />
          <input id="crm-task-title" type="text" value={values.title} onChange={(e) => setValues({ ...values, title: e.target.value })} />
          {errors.title && <p className="form-error">{errors.title}</p>}
        </div>

        <div className="row">
          <div>
            <label htmlFor="crm-task-duedate">Due date</label>
            <br />
            <input id="crm-task-duedate" type="date" value={values.dueDate} onChange={(e) => setValues({ ...values, dueDate: e.target.value })} />
            {errors.dueDate && <p className="form-error">{errors.dueDate}</p>}
          </div>
          <div>
            <label htmlFor="crm-task-duetime">Due time (optional)</label>
            <br />
            <input id="crm-task-duetime" type="time" value={values.dueTime} onChange={(e) => setValues({ ...values, dueTime: e.target.value })} />
          </div>
          <div>
            <label htmlFor="crm-task-priority">Priority</label>
            <br />
            <select id="crm-task-priority" value={values.priority} onChange={(e) => setValues({ ...values, priority: e.target.value })}>
              {CRM_TASK_PRIORITIES.map((priority) => (
                <option key={priority} value={priority}>
                  {priority}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div>
          <label htmlFor="crm-task-assignee">Assigned to</label>
          <br />
          <select id="crm-task-assignee" value={values.assignedUserId} onChange={(e) => setValues({ ...values, assignedUserId: e.target.value })}>
            <option value="">Select an administrator...</option>
            {administrators.map((admin) => (
              <option key={admin.id} value={admin.id}>
                {admin.firstName} {admin.lastName}
              </option>
            ))}
          </select>
          {errors.assignedUserId && <p className="form-error">{errors.assignedUserId}</p>}
        </div>

        <div>
          <label htmlFor="crm-task-description">Description</label>
          <br />
          <textarea id="crm-task-description" rows={3} value={values.description} onChange={(e) => setValues({ ...values, description: e.target.value })} />
        </div>

        {submitError && <p role="alert">{submitError}</p>}

        <div style={{ marginTop: "1rem" }}>
          <button type="button" onClick={onClose} disabled={isSaving}>
            Cancel
          </button>{" "}
          <button type="submit" disabled={isSaving}>
            {isSaving ? "Saving..." : "Save"}
          </button>
        </div>
      </form>
    </Modal>
  );
}

export default TaskFormModal;
