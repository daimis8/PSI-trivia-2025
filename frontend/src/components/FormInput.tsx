interface FormInputProps {
  label: string;
  type: string;
  value: string;
  onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  required?: boolean;
  placeholder?: string;
}

export function FormInput({
  label,
  type,
  value,
  onChange,
  placeholder,
}: FormInputProps) {
  return (
    <div className="flex flex-col gap-2">
      <label className="font-medium">{label}</label>
      <input
        type={type}
        value={value}
        onChange={onChange}
        placeholder={placeholder}
        className="rounded border px-3 py-2 focus:border-blue-500 focus:outline-none"
      />
    </div>
  );
}
