import * as React from "react"

function Button({
  className,
  children,
  onClick,
}: React.ComponentProps<"button"> &
  {
  }) {

  return (
    <button
      className={className}
      onClick={onClick}
    >
      {children}
    </button>
  )
}

export { Button }
