import { useState } from "react";
import { useAuth } from "@/context/AuthContext";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Separator } from "@/components/ui/separator";
import { AlertBox } from "@/components/AlertBox";
import {
  validateEmail,
  validatePassword,
  validateConfirmPassword,
  validateUsername,
} from "@/lib/validation";
import { apiFetch } from "@/lib/api";

interface EditProfileDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function EditProfileDialog({
  open,
  onOpenChange,
}: EditProfileDialogProps) {
  const { user, refetchAuth } = useAuth();
  const [username, setUsername] = useState(user?.username || "");
  const [email, setEmail] = useState(user?.email || "");
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [usernameError, setUsernameError] = useState("");
  const [emailError, setEmailError] = useState("");
  const [passwordError, setPasswordError] = useState("");
  const [confirmPasswordError, setConfirmPasswordError] = useState("");
  const [serverError, setServerError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  const handleOpenChange = (isOpen: boolean) => {
    if (isOpen) {
      setUsername(user?.username || "");
      setEmail(user?.email || "");
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
      setUsernameError("");
      setEmailError("");
      setPasswordError("");
      setConfirmPasswordError("");
      setServerError("");
      setSuccessMessage("");
    }
    onOpenChange(isOpen);
  };

  const handleSaveChanges = async () => {
    setIsLoading(true);
    setUsernameError("");
    setEmailError("");
    setPasswordError("");
    setConfirmPasswordError("");
    setServerError("");
    setSuccessMessage("");

    let hasValidationErrors = false;
    let hasChanges = false;

    if (username !== user?.username) {
      const usernameValidation = validateUsername(username);
      if (usernameValidation) {
        setUsernameError(usernameValidation);
        hasValidationErrors = true;
      }
    }

    if (email !== user?.email) {
      const emailValidation = validateEmail(email);
      if (emailValidation) {
        setEmailError(emailValidation);
        hasValidationErrors = true;
      }
    }

    if (currentPassword || newPassword || confirmPassword) {
      const passwordValidation = validatePassword(newPassword);
      const confirmPasswordValidation = validateConfirmPassword(
        newPassword,
        confirmPassword
      );

      if (passwordValidation) {
        setPasswordError(passwordValidation);
        hasValidationErrors = true;
      }
      if (confirmPasswordValidation) {
        setConfirmPasswordError(confirmPasswordValidation);
        hasValidationErrors = true;
      }
      if (!currentPassword) {
        setPasswordError("Current password is required");
        hasValidationErrors = true;
      }
    }

    if (hasValidationErrors) {
      setIsLoading(false);
      return;
    }

    if (username !== user?.username) {
      try {
        const response = await apiFetch("/api/users/username", {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ username }),
        });

        const data = await response.json();

        if (!response.ok) {
          setServerError(data.message);
          setIsLoading(false);
          return;
        }
        hasChanges = true;
      } catch {
        setServerError("An error occurred while updating username");
        setIsLoading(false);
        return;
      }
    }

    if (email !== user?.email) {
      try {
        const response = await apiFetch("/api/users/email", {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email }),
        });

        const data = await response.json();

        if (!response.ok) {
          setServerError(data.message);
          setIsLoading(false);
          return;
        }
        hasChanges = true;
      } catch {
        setServerError("An error occurred while updating email");
        setIsLoading(false);
        return;
      }
    }

    if (currentPassword && newPassword) {
      try {
        const response = await apiFetch("/api/users/password", {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            currentPassword,
            newPassword,
          }),
        });

        const data = await response.json();

        if (!response.ok) {
          setServerError(data.message);
          setIsLoading(false);
          return;
        }
        hasChanges = true;
      } catch {
        setServerError("An error occurred while updating password");
        setIsLoading(false);
        return;
      }
    }

    if (hasChanges) {
      setSuccessMessage("Profile updated successfully!");
      refetchAuth();
      setTimeout(() => {
        handleOpenChange(false);
      }, 1500);
    }

    setIsLoading(false);
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>Edit Profile</DialogTitle>
          <DialogDescription>
            Update your profile information. Changes will be saved when you
            click save.
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-6 py-4">
          {(usernameError ||
            emailError ||
            passwordError ||
            confirmPasswordError ||
            serverError) && (
              <AlertBox
                isLogin={false}
                emailError={emailError}
                passwordError={passwordError}
                confirmPasswordError={confirmPasswordError}
                usernameError={usernameError}
                serverError={serverError}
              />
            )}

          {successMessage && (
            <div className="bg-green-50 border border-green-200 text-green-800 px-4 py-3 rounded-lg">
              <p className="text-sm font-medium">{successMessage}</p>
            </div>
          )}

          <div className="grid gap-2">
            <Label htmlFor="username">Username</Label>
            <Input
              id="username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="Enter username"
              disabled={isLoading}
              className={usernameError ? "border-red-500" : ""}
            />
          </div>

          <div className="grid gap-2">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="Enter email"
              disabled={isLoading}
              className={emailError ? "border-red-500" : ""}
            />
          </div>

          <Separator />

          <div className="grid gap-4">
            <div className="grid gap-2">
              <Label htmlFor="currentPassword">Current Password</Label>
              <Input
                id="currentPassword"
                type="password"
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                placeholder="Enter current password"
                disabled={isLoading}
                className={passwordError ? "border-red-500" : ""}
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="newPassword">New Password</Label>
              <Input
                id="newPassword"
                type="password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                placeholder="Enter new password"
                disabled={isLoading}
                className={passwordError ? "border-red-500" : ""}
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="confirmPassword">Confirm New Password</Label>
              <Input
                id="confirmPassword"
                type="password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="Confirm new password"
                disabled={isLoading}
                className={confirmPasswordError ? "border-red-500" : ""}
              />
            </div>
          </div>
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => handleOpenChange(false)}
            disabled={isLoading}
          >
            Cancel
          </Button>
          <Button onClick={handleSaveChanges} disabled={isLoading}>
            {isLoading ? "Saving..." : "Save changes"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
