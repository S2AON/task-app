import React from "react";

interface SpinnerProps {
  size?: "sm" | "md" | "lg";
  fullScreen?: boolean;
}

export const Spinner: React.FC<SpinnerProps> = ({
  size = "md",
  fullScreen = false,
}) => {
  const sizes = {
    sm: "h-4 w-4",
    md: "h-8 w-8",
    lg: "h-12 w-12",
  };

  const spinner = (
    <div
      className={`animate-spin rounded-full border-b-2 border-blue-500 ${sizes[size]}`}
    ></div>
  );

  if (fullScreen) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        {spinner}
      </div>
    );
  }

  return spinner;
};
