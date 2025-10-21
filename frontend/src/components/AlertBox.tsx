import { AlertCircleIcon } from "lucide-react";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";

interface AlertBoxProps {
  isLogin: boolean;
  emailError: string;
  passwordError: string;
  confirmPasswordError?: string;
  usernameError: string;
  serverError?: string;
}

export function AlertBox({
  isLogin,
  emailError,
  passwordError,
  confirmPasswordError,
  usernameError,
  serverError,
}: AlertBoxProps) {
  const errors = [
    emailError,
    passwordError,
    confirmPasswordError,
    usernameError,
    serverError,
  ].filter(Boolean);

  if (errors.length === 0) return null;

  return (
    <div className="mb-4">
      <Alert variant="destructive">
        <AlertCircleIcon />
        <AlertTitle>
          {isLogin ? "Unable to sign in" : "Unable to create an account"}
        </AlertTitle>
        <AlertDescription>
          {serverError ? (
            <p>{serverError}</p>
          ) : (
            <>
              <p>
                {isLogin
                  ? "Please verify your login information and try again."
                  : "Please verify your registration information and try again."}
              </p>
              <ul className="list-inside list-disc text-sm mt-2">
                {emailError && <li>{emailError}</li>}
                {passwordError && <li>{passwordError}</li>}
                {confirmPasswordError && <li>{confirmPasswordError}</li>}
                {usernameError && <li>{usernameError}</li>}
              </ul>
            </>
          )}
        </AlertDescription>
      </Alert>
    </div>
  );
}
