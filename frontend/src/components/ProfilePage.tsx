import { useState } from "react";
import { useAuth } from "@/context/AuthContext";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { EditProfileDialog } from "@/components/EditProfileDialog";
import { Star, Users, Edit } from "lucide-react";

export function ProfilePage() {
  const { user } = useAuth();
  const [isDialogOpen, setIsDialogOpen] = useState(false);

  const stats = [
    {
      icon: Star,
      value: "128",
      label: "Quizzes Completed",
    },
    {
      icon: Users,
      value: "8.5k",
      label: "Quizzes Created",
    },
    {
      icon: Star,
      value: "99%",
      label: "Average Score",
    },
  ];

  const activities = [
    {
      title: 'Completed quiz "Quiz 1"',
      time: "2 hours ago",
    },
    {
      title: 'Completed quiz "Quiz 2"',
      time: "2 hours ago",
    },
    {
      title: 'Completed quiz "Quiz 3"',
      time: "2 hours ago",
    },
  ];

  return (
    <div className="p-8">
      <div className="flex justify-between items-center mb-8">
        <h1 className="text-4xl font-bold tracking-tight">
          Profile
        </h1>

        <Button
          variant="outline"
          className="gap-2"
          onClick={() => setIsDialogOpen(true)}
        >
          <Edit className="size-4" />
          Edit Profile
        </Button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-1">
          <Card className="bg-card">
            <CardContent className="flex flex-col items-center text-center pt-6 gap-2">
              <Avatar className="size-32 mb-4">
                <AvatarImage src="https://github.com/shadcn.png" />
                <AvatarFallback className="text-2xl bg-primary text-primary-foreground">
                  {user?.username}
                </AvatarFallback>
              </Avatar>

              <h2 className="text-2xl font-bold mb-1">
                {user?.username}
              </h2>
              <Badge variant="secondary" className="mb-6">
                Pro quizzer
              </Badge>
            </CardContent>
          </Card>
        </div>

        <div className="lg:col-span-2 space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {stats.map((stat, index) => (
              <Card
                key={index}
                className="hover:shadow-md transition-shadow shadow-white bg-card"
              >
                <CardContent className="flex items-center gap-4 py-8">
                  <div className="bg-card-darker p-3 rounded-lg">
                    <stat.icon className="size-6 text-muted-foreground" />
                  </div>
                  <div>
                    <div className="text-3xl font-bold mb-1">
                      {stat.value}
                    </div>
                    <div className="text-muted-foreground text-sm">
                      {stat.label}
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
          <Card className="bg-card-dark">
            <CardHeader>
              <CardTitle>Recent Activity</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {activities.map((activity, index) => (
                  <div key={index} className="flex items-start gap-4">
                    <div className="bg-card-darker p-2 rounded-lg mt-1">
                      <Star className="size-5 text-muted-foreground" />
                    </div>
                    <div className="flex-1">
                      <p className="font-medium mb-1">
                        {activity.title}
                      </p>
                      <p className="text-sm text-muted-foreground">
                        {activity.time}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      <EditProfileDialog open={isDialogOpen} onOpenChange={setIsDialogOpen} />
    </div>
  );
}
