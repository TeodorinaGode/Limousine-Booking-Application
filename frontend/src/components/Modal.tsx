import type { ReactNode } from "react";

interface ModalProps {
  title: string;
  onClose: () => void;
  children: ReactNode;
}

const overlayStyle: React.CSSProperties = {
  position: "fixed",
  inset: 0,
  backgroundColor: "rgba(0, 0, 0, 0.7)",
  backdropFilter: "blur(2px)",
  display: "flex",
  alignItems: "center",
  justifyContent: "center",
  zIndex: 1000,
  padding: "var(--space-4)",
};

const contentStyle: React.CSSProperties = {
  background: "var(--color-surface)",
  border: "1px solid var(--color-border-light)",
  borderRadius: "var(--radius-lg)",
  boxShadow: "var(--shadow-lg)",
  padding: "var(--space-6)",
  minWidth: 320,
  maxWidth: "90vw",
  maxHeight: "90vh",
  overflowY: "auto",
};

/** All dialogs use this one dark surface (section 32) — every existing form modal wraps its content in this component. */
function Modal({ title, onClose, children }: ModalProps) {
  return (
    <div
      style={overlayStyle}
      role="presentation"
      className="fade-in"
      onClick={(event) => {
        if (event.target === event.currentTarget) onClose();
      }}
    >
      <div style={contentStyle} role="dialog" aria-modal="true" aria-label={title} className="fade-in">
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "var(--space-4)" }}>
          <h2 style={{ margin: 0, border: "none", padding: 0 }}>{title}</h2>
          <button type="button" className="btn-ghost" onClick={onClose} aria-label="Close" style={{ fontSize: "1.1rem", padding: "0.2em 0.6em" }}>
            ×
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}

export default Modal;
