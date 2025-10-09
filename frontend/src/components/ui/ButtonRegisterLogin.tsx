import * as React from "react";

function ButtonRegisterLogin({
  className,
  children,
  ...props
}: React.ComponentProps<"button"> & {}) {
  return (
    <button className={className} {...props}>
      {children}
    </button>
  );
}

export { ButtonRegisterLogin };
