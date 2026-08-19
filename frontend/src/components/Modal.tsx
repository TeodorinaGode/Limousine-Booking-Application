import type { ReactNode } from "react";

interface ModalProps {
  title: string;
  onClose: () => void;
  children: ReactNode;
}

const overlayStyle: React.CSSProperties = {
  position: "fixed",
  inset: 0,
  backgroundColor: "rgba(0, 0, 0, 0.5)",
  display: "flex",
  alignItems: "center",
  justifyContent: "center",
  zIndex: 1000,
};

const contentStyle: React.CSSProperties = {
  background: "white",
  borderRadius: 8,
  padding: "1.5rem",
  minWidth: 320,
  maxWidth: "90vw",
  maxHeight: "90vh",
  overflowY: "auto",
};

function Modal({ title, onClose, children }: ModalProps) {
  return (
    <div
      style={overlayStyle}
      role="presentation"
      onClick={(event) => {
        if (event.target === event.currentTarget) onClose();
      }}
    >
      <div style={contentStyle} role="dialog" aria-modal="true" aria-label={title}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "1rem" }}>
          <h2 style={{ margin: 0 }}>{title}</h2>
          <button type="button" onClick={onClose} aria-label="Close">
            ×
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}

export default Modal;
