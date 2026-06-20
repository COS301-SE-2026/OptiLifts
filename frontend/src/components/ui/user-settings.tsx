import React, { useEffect, useRef, useState } from "react";
import { Eye, EyeOff, ImagePlus, Loader2, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useAuth } from "@/context/auth-context";
import { toast } from "@/components/ui/alert";
import { CircularProfileImage } from "@/components/ui/circular-image";
import { customFetch } from "@/lib/custom-fetch";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

type UserSettingsPopupProps = Readonly<{
    isOpen: boolean;
    onClose: () => void;
}>;

type ProfileParams = Readonly<{
    profile: {
        displayName: string;
        bio: string;
        sex: string;
        dateOfBirth: string;
        weight: string;
        height: string;
    };
    updateProfile: (field: string, value: string) => void;
    selectedImgUrl: string | null;
    setSelectedImg: (file: File | null) => void;
    setSelectedImgUrl: (url: string | null) => void;
}>;

type PreferencesParams = Readonly<{
    preferences: {
        theme: string;
        units: string;
    };
    updatePreferences: (field: string, value: string) => void;
}>;

type SecurityParams = Readonly<{
    security: {
        currentPassword: string;
        newPassword: string;
        confirmPassword: string;
    };
    updateSecurity: (field: string, value: string) => void;
}>;

interface UserSettingsDto {
    profile: {
        displayName: string;
        bio: string;
        sex: string;
        dateOfBirth: string;
        weight: number;
        height: number;
        profilePictureUrl: string | null;
    };

    preferences: {
        theme: string;
        units: string;
    };

}



function useSettingsLogic(isOpen: boolean, onClose: () => void) {
    const { user } = useAuth();

    //states 
    const [profile, setProfile] = useState({
        displayName: "",
        bio: "",
        sex: "",
        dateOfBirth: "",
        weight: "",
        height: ""
    });

    const [preferences, setPreferences] = useState({
        theme: "dark",
        units: "metric"
    });

    const [security, setSecurity] = useState({
        currentPassword: "",
        newPassword: "",
        confirmPassword: ""
    });

    const [selectedImg, setSelectedImg] = useState<File | null>(null);
    const [selectedImgUrl, setSelectedImgUrl] = useState<string | null>(null);

    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [isSaving, setIsSaving] = useState<boolean>(false);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);

    // takes field and uses it as key to update state w/ new value 
    // prev => ...prev takes the old object and makes a new one with the old values but updates the new value
    const updateProfile = (field: string, value: string) => setProfile(prev => ({ ...prev, [field]: value }));
    const updatePreferences = (field: string, value: string) => setPreferences(prev => ({ ...prev, [field]: value }));
    const updateSecurity = (field: string, value: string) => setSecurity(prev => ({ ...prev, [field]: value }));

    useEffect(() => {
        if (!isOpen) {
            return;
        }

        async function getSettings() {
            setIsLoading(true);
            setErrorMessage(null);

            try {
                const response = await customFetch("/api/users/me/settings");
                if (!response.ok) {
                    throw new Error("Failed to load settings");
                }

                const data: UserSettingsDto = await response.json();

                setProfile({
                    displayName: data.profile.displayName,
                    bio: data.profile.bio || "",
                    sex: data.profile.sex || "PreferNotToSay",
                    dateOfBirth: data.profile.dateOfBirth ? data.profile.dateOfBirth.split('T')[0] : "",
                    weight: (data.profile.weight) ? data.profile.weight.toString() : "",
                    height: (data.profile.height) ? data.profile.height.toString() : ""
                });

                setSelectedImgUrl(data.profile.profilePictureUrl);

                setPreferences({
                    theme: data.preferences.theme || "dark",
                    units: data.preferences.units || "metric"
                });

            } catch (error) {
                const typedError = (error instanceof Error) ? error : new Error("Could not load settings");
                setErrorMessage(typedError.message);
            } finally {
                setIsLoading(false);
            }

            setSecurity({
                currentPassword: "",
                newPassword: "",
                confirmPassword: ""
            });

            setSelectedImg(null);
        }

        getSettings();

    }, [isOpen, user]);

    const saveProfileDetails = async () => {
        const res = await customFetch("/api/users/me/personalDetails", {
            method: "PATCH",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                ...profile,
                weight: (profile.weight == "") ? null : Number.parseFloat(profile.weight),
                height: (profile.height == "") ? null : Number.parseFloat(profile.height)
            })
        });

        if (!res.ok) {
            throw new Error("Could not update profile information.");
        }
    };

    const savePreferences = async () => {
        const res = await customFetch("/api/users/me/preferences", {
            method: "PATCH",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(preferences)
        });

        if (!res.ok) {
            throw new Error("Could not update preferences.");
        }
    };

    const savePassword = async () => {
        if (security.newPassword === "" || security.currentPassword === "") {
            return;
        }

        const res = await customFetch("/api/users/me/change-password", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                currentPassword: security.currentPassword,
                newPassword: security.newPassword
            })
        });

        if (!res.ok) {
            throw new Error("Could not change password, please check your current password.");
        }
    };

    const saveProfilePic = async () => {
        if (!selectedImg) {
            return;
        }

        //creates an object passed via multipart/mixed, more efficient than base64 json
        const formData = new FormData();
        formData.append("profilePicture", selectedImg);
        const res = await customFetch("/api/users/me/profilepicture", {
            method: "PATCH",
            body: formData
        });

        if (!res.ok) {
            throw new Error("Failed to upload profile picture.");
        }
    };

    const handleSave = async (e: React.SyntheticEvent<HTMLFormElement>) => {
        e.preventDefault();
        setErrorMessage(null);

        if (security.newPassword !== "") {
            if (security.newPassword !== security.confirmPassword) {
                setErrorMessage("New passwords do not match.");
                return;
            }

            if (security.currentPassword === "") {
                setErrorMessage("Enter current password");
                return;
            }
        }

        setIsSaving(true);
        try {
            await saveProfileDetails();
            await saveProfilePic();
            await savePreferences();
            await savePassword();

            if (preferences.theme === "dark") {
                document.documentElement.classList.add("dark");
            } else {
                document.documentElement.classList.remove("dark");
            }

            toast.success("Settings saved successfully", "Saved");
            onClose();

        } catch (error) {
            const typedError = (error instanceof Error) ? error : new Error("Unknown error occurred");
            setErrorMessage(typedError.message);
        } finally {
            setIsSaving(false);
        }
    };

    return {
        profile, updateProfile,
        preferences, updatePreferences,
        security, updateSecurity,
        selectedImgUrl, setSelectedImg, setSelectedImgUrl,
        isLoading, isSaving, errorMessage, handleSave
    };
}


function ProfileSection({ profile, updateProfile, selectedImgUrl, setSelectedImg, setSelectedImgUrl }: ProfileParams) {
    const fileInputRef = useRef<HTMLInputElement | null>(null);

    const handleImgChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (file) {
            //delete old image in memory
            if (selectedImgUrl?.startsWith("blob:")) {
                URL.revokeObjectURL(selectedImgUrl);
            }

            setSelectedImg(file);
            const newUrl = URL.createObjectURL(file);
            setSelectedImgUrl(newUrl);
        }
    };

    const handleRemoveImage = () => {
        if (selectedImgUrl?.startsWith("blob:")) {
            URL.revokeObjectURL(selectedImgUrl);
        }

        setSelectedImg(null);
        setSelectedImgUrl(null);

        if (fileInputRef.current) {
            fileInputRef.current.value = "";
        }
    };

    return (
        <div className="space-y-4">
            <h3 className="font-bold border-b border-border pb-1 text-foreground uppercase tracking-wider text-base">Profile Details</h3>

            <div className="flex flex-col items-center justify-center pb-2">
                <span className="text-xs font-bold uppercase tracking-[1.5px] text-muted-foreground mb-3">Profile Picture</span>
                <div className="group relative flex h-24 w-24 items-center justify-center rounded-full border border-border bg-surface-2 transition-all duration-300">
                    <CircularProfileImage
                        src={selectedImgUrl || undefined}
                        alt={profile.displayName}
                        className="h-full w-full object-cover rounded-full"
                    />
                    <button
                        type="button"
                        onClick={() => fileInputRef.current?.click()}
                        className="absolute inset-0 flex flex-col items-center justify-center rounded-full bg-black/60 opacity-0 group-hover:opacity-100 transition-opacity duration-200 cursor-pointer text-white"
                        aria-label="Change profile picture"
                    >
                        <ImagePlus size={20} className="mb-1" />
                        <span className="text-[10px] font-semibold uppercase tracking-[0.5px]">Upload</span>
                    </button>
                    <input
                        ref={fileInputRef}
                        type="file"
                        accept="image/*"
                        className="sr-only"
                        onChange={handleImgChange}
                    />
                </div>
                {selectedImgUrl && (
                    <Button
                        type="button"
                        variant="ghost"
                        onClick={handleRemoveImage}
                        className="mt-2.5 h-7 px-3 text-xs text-brand hover:text-brand-2 hover:bg-brand/5 rounded-md"
                    >
                        Remove Picture
                    </Button>
                )}
            </div>

            <div className="flex flex-col gap-1.5">
                <span className="text-xs font-bold text-muted-foreground">Display Name</span>
                <Input value={profile.displayName} onChange={(e) => updateProfile("displayName", e.target.value)} required />
            </div>

            <div className="flex flex-col gap-1.5">
                <span className="text-xs font-bold text-muted-foreground">Bio</span>
                <textarea
                    value={profile.bio}
                    onChange={(e) => updateProfile("bio", e.target.value)}
                    className="w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm text-foreground outline-none focus-visible:ring-1 focus-visible:ring-ring"
                    rows={2}
                />
            </div>

            <div className="grid grid-cols-2 gap-4">
                <div className="flex flex-col gap-1.5">
                    <span className="text-xs font-bold text-muted-foreground">Sex</span>
                    <DropdownMenu>
                        <DropdownMenuTrigger 
                            variant="filter" 
                            className="w-full bg-transparent dark:bg-input/30 rounded-lg border-input h-8 py-1 px-2.5"
                        >
                            {profile.sex === "PreferNotToSay" ? "Prefer not to say" : profile.sex}
                        </DropdownMenuTrigger>
                        <DropdownMenuContent className="w-[var(--radix-dropdown-menu-trigger-width)]">
                            {["Male", "Female", "Other", "PreferNotToSay"].map((option) => (
                                <DropdownMenuItem key={option} onClick={() => updateProfile("sex", option)}>
                                    {option === "PreferNotToSay" ? "Prefer not to say" : option}
                                </DropdownMenuItem>
                            ))}
                        </DropdownMenuContent>
                    </DropdownMenu>
                </div>
                <div className="flex flex-col gap-1.5">
                    <span className="text-xs font-bold text-muted-foreground">Date of Birth</span>
                    <Input type="date" value={profile.dateOfBirth} onChange={(e) => updateProfile("dateOfBirth", e.target.value)} />
                </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
                <div className="flex flex-col gap-1.5">
                    <span className="text-xs font-bold text-muted-foreground">Weight</span>
                    <Input type="number" step="0.1" value={profile.weight} onChange={(e) => updateProfile("weight", e.target.value)} />
                </div>
                <div className="flex flex-col gap-1.5">
                    <span className="text-xs font-bold text-muted-foreground">Height</span>
                    <Input type="number" step="0.1" value={profile.height} onChange={(e) => updateProfile("height", e.target.value)} />
                </div>
            </div>
        </div>
    );
}

function PreferencesSection({ preferences, updatePreferences }: PreferencesParams) {
    return (
        <div className="space-y-4">
            <h3 className="font-bold border-b border-border pb-1 text-foreground uppercase tracking-wider text-base">App Preferences</h3>
            <div className="flex items-center justify-between">
                <span className="text-sm">Theme</span>
                <DropdownMenu>
                    <DropdownMenuTrigger 
                        variant="filter" 
                        className="w-48 bg-transparent dark:bg-input/30 rounded-lg border-input h-8 py-1 px-2.5"
                    >
                        {preferences.theme === "light" ? "Light Mode" : "Dark Mode"}
                    </DropdownMenuTrigger>
                    <DropdownMenuContent className="w-[var(--radix-dropdown-menu-trigger-width)]">
                        <DropdownMenuItem onClick={() => updatePreferences("theme", "light")}>Light Mode</DropdownMenuItem>
                        <DropdownMenuItem onClick={() => updatePreferences("theme", "dark")}>Dark Mode</DropdownMenuItem>
                    </DropdownMenuContent>
                </DropdownMenu>
            </div>
            <div className="flex items-center justify-between">
                <span className="text-sm">Units</span>
                <DropdownMenu>
                    <DropdownMenuTrigger 
                        variant="filter" 
                        className="w-48 bg-transparent dark:bg-input/30 rounded-lg border-input h-8 py-1 px-2.5"
                    >
                        {preferences.units === "metric" ? "Metric (kg / cm)" : "Imperial (lbs / in)"}
                    </DropdownMenuTrigger>
                    <DropdownMenuContent className="w-[var(--radix-dropdown-menu-trigger-width)]">
                        <DropdownMenuItem onClick={() => updatePreferences("units", "metric")}>Metric (kg / cm)</DropdownMenuItem>
                        <DropdownMenuItem onClick={() => updatePreferences("units", "imperial")}>Imperial (lbs / in)</DropdownMenuItem>
                    </DropdownMenuContent>
                </DropdownMenu>
            </div>
        </div>
    );
}

function SecuritySection({ security, updateSecurity }: SecurityParams) {
    const [showCurrent, setShowCurrent] = useState<boolean>(false);
    const [showNew, setShowNew] = useState<boolean>(false);
    const [showConfirm, setShowConfirm] = useState<boolean>(false);

    return (
        <div className="space-y-4">
            <h3 className="font-bold border-b border-border pb-1 text-foreground uppercase tracking-wider text-base">Change Password</h3>

            <div className="flex flex-col gap-1.5">
                <span className="text-xs font-bold text-muted-foreground">Current Password</span>
                <div className="relative w-full">
                    <Input
                        type={showCurrent ? "text" : "password"}
                        value={security.currentPassword}
                        onChange={(e) => updateSecurity("currentPassword", e.target.value)}
                        className="pr-11"
                    />
                    <Button
                        type="button" variant="password" size="icon"
                        onClick={() => setShowCurrent(!showCurrent)}
                        className="absolute right-1 top-1/2 -translate-y-1/2"
                    >
                        {showCurrent ? <Eye size={16} /> : <EyeOff size={16} />}
                    </Button>
                </div>
            </div>

            <div className="flex flex-col gap-1.5">
                <span className="text-xs font-bold text-muted-foreground">New Password</span>
                <div className="relative w-full">
                    <Input
                        type={showNew ? "text" : "password"}
                        value={security.newPassword}
                        onChange={(e) => updateSecurity("newPassword", e.target.value)}
                        className="pr-11"
                    />
                    <Button
                        type="button" variant="password" size="icon"
                        onClick={() => setShowNew(!showNew)}
                        className="absolute right-1 top-1/2 -translate-y-1/2"
                    >
                        {showNew ? <Eye size={16} /> : <EyeOff size={16} />}
                    </Button>
                </div>
            </div>

            <div className="flex flex-col gap-1.5">
                <span className="text-xs font-bold text-muted-foreground">Confirm New Password</span>
                <div className="relative w-full">
                    <Input
                        type={showConfirm ? "text" : "password"}
                        value={security.confirmPassword}
                        onChange={(e) => updateSecurity("confirmPassword", e.target.value)}
                        className="pr-11"
                    />
                    <Button
                        type="button" variant="password" size="icon"
                        onClick={() => setShowConfirm(!showConfirm)}
                        className="absolute right-1 top-1/2 -translate-y-1/2"
                    >
                        {showConfirm ? <Eye size={16} /> : <EyeOff size={16} />}
                    </Button>
                </div>
            </div>
        </div>
    );
}

export function UserSettingsPopup({ isOpen, onClose }: UserSettingsPopupProps) {
    const {
        profile, updateProfile,
        preferences, updatePreferences,
        security, updateSecurity,
        selectedImgUrl, setSelectedImg, setSelectedImgUrl,
        isLoading, isSaving, errorMessage, handleSave
    } = useSettingsLogic(isOpen, onClose);

    if (!isOpen) {
        return null;
    }

    const handleClosePopup = () => {
        // upload img but cancle
        if (selectedImgUrl?.startsWith("blob:")) {
            URL.revokeObjectURL(selectedImgUrl);
        }
        
        setSelectedImg(null);
        setSelectedImgUrl(null);
        
        onClose(); 
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
            <div className="w-full max-w-lg bg-surface border border-border rounded-xl shadow-lg flex flex-col max-h-[85vh] overflow-hidden animate-in fade-in zoom-in-95 duration-200">

                <div className="flex items-center justify-between border-b border-border p-4">
                    <h2 className="text-xl font-bold font-display uppercase tracking-wider text-foreground">Settings</h2>
                    <button onClick={handleClosePopup} className="text-muted-foreground hover:text-foreground cursor-pointer">
                        <X size={20} />
                    </button>
                </div>

                {isLoading ? (
                    <div className="p-8 text-center text-muted-foreground flex flex-col items-center justify-center gap-2">
                        <Loader2 className="animate-spin text-brand" />
                        <span>Loading settings...</span>
                    </div>
                ) : (
                    <form onSubmit={handleSave} className="flex-1 overflow-y-auto p-5 space-y-6">

                        {errorMessage && (
                            <div className="bg-destructive/10 border border-destructive/20 text-destructive text-sm p-3 rounded-lg">
                                {errorMessage}
                            </div>
                        )}

                        <ProfileSection
                            profile={profile} updateProfile={updateProfile}
                            selectedImgUrl={selectedImgUrl}
                            setSelectedImg={setSelectedImg}
                            setSelectedImgUrl={setSelectedImgUrl}
                        />

                        <PreferencesSection
                            preferences={preferences} updatePreferences={updatePreferences}
                        />

                        <SecuritySection
                            security={security} updateSecurity={updateSecurity}
                        />

                        <div className="flex justify-end gap-3 pt-4 border-t border-border">
                            <Button type="button" variant="secondary" onClick={handleClosePopup} disabled={isSaving}>Cancel</Button>
                            <Button type="submit" disabled={isSaving}>{isSaving ? "Saving..." : "Save Changes"}</Button>
                        </div>
                    </form>
                )}
            </div>
        </div>
    );
}